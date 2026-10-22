using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RevitCodexChat
{
    // Uses existing Codex CLI login. No API keys or tokens are read by this add-in.
    public static class CodexClient
    {
        public static Task<string> AskAsync(string prompt, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                string executable = FindExecutable();
                string work = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RevitCodexChat", "workspace");
                Directory.CreateDirectory(work);
                var start = new ProcessStartInfo
                {
                    FileName = executable,
                    // No shell, user plugins, MCP servers, hooks or project context.
                    // Read-only sandbox is a second boundary, not the sole model protection.
                    Arguments = "exec --json --ephemeral --skip-git-repo-check --ignore-user-config --sandbox read-only --color never "
                        + "--disable shell_tool --disable unified_exec --disable apps --disable plugins --disable hooks "
                        + "--disable browser_use --disable computer_use --disable image_generation -",
                    WorkingDirectory = work,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                // Some launchers supply both Path and PATH; normalize before starting
                // a child .NET/CLI process. Never log environment values.
                start.EnvironmentVariables.Clear();
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                    start.EnvironmentVariables[(string)entry.Key] = (string)entry.Value;
                using (var process = new Process { StartInfo = start })
                {
                    if (!process.Start()) throw new InvalidOperationException("Không thể mở Codex CLI.");
                    using (token.Register(() =>
                    {
                        try { if (!process.HasExited) process.Kill(); }
                        catch (InvalidOperationException) { }
                        catch (System.ComponentModel.Win32Exception) { }
                    }))
                    {
                        // Drain stderr concurrently so the subprocess cannot deadlock.
                        var errors = ReadErrors(process.StandardError);
                        try
                        {
                            using (var input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
                            { await input.WriteAsync(prompt).ConfigureAwait(false); }
                            var answer = new StringBuilder();
                            string failure = null;
                            var json = new JavaScriptSerializer { MaxJsonLength = 2 * 1024 * 1024 };
                            string line;
                            while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                            {
                                token.ThrowIfCancellationRequested();
                                Dictionary<string, object> evt;
                                try { evt = json.Deserialize<Dictionary<string, object>>(line); }
                                catch (ArgumentException) { continue; }
                                if (evt == null) continue;
                                string type = Value(evt, "type");
                                if (type == "item.completed" && evt.TryGetValue("item", out object itemValue)
                                    && itemValue is Dictionary<string, object> item && Value(item, "type") == "agent_message")
                                {
                                    if (answer.Length > 0) answer.AppendLine();
                                    answer.Append(Value(item, "text"));
                                    if (answer.Length > 100000) throw new InvalidOperationException("Phản hồi quá dài. Hãy đặt câu hỏi ngắn hơn.");
                                }
                                if (type == "error") failure = Value(evt, "message");
                                if (type == "turn.failed")
                                {
                                    if (evt.TryGetValue("error", out object error) && error is Dictionary<string, object> detail)
                                        failure = Value(detail, "message");
                                    else failure = "Codex không thể hoàn tất yêu cầu.";
                                }
                            }
                            process.WaitForExit();
                            string stderr = await errors.ConfigureAwait(false);
                            token.ThrowIfCancellationRequested();
                            if (process.ExitCode != 0 || !string.IsNullOrEmpty(failure))
                                throw new InvalidOperationException("Codex không thể trả lời. Kiểm tra kết nối, hạn mức và đăng nhập bằng codex login.\n"
                                    + Redact(failure ?? stderr));
                            if (answer.Length == 0) throw new InvalidOperationException("Codex không trả về nội dung. Kiểm tra đăng nhập và phiên bản CLI.");
                            return answer.ToString();
                        }
                        catch
                        {
                            try { if (!process.HasExited) process.Kill(); }
                            catch (InvalidOperationException) { }
                            catch (System.ComponentModel.Win32Exception) { }
                            await errors.ConfigureAwait(false);
                            token.ThrowIfCancellationRequested();
                            throw;
                        }
                    }
                }
            }, token);
        }

        private static async Task<string> ReadErrors(StreamReader reader)
        {
            var buffer = new char[2048];
            var text = new StringBuilder();
            int count;
            while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                text.Append(buffer, 0, count);
                if (text.Length > 6000) text.Remove(0, text.Length - 6000);
            }
            return text.ToString();
        }

        private static string Value(Dictionary<string, object> item, string key) => item.TryGetValue(key, out object value) ? value as string : null;
        private static string Redact(string text)
        {
            text = Regex.Replace(text ?? "", @"(?i)(sk-[a-z0-9_-]+|Bearer\s+\S+)", "[đã ẩn]");
            return text.Length > 1500 ? text.Substring(0, 1500) + "…" : text;
        }

        public static string FindExecutable()
        {
            string custom = Environment.GetEnvironmentVariable("REVIT_CODEX_EXE");
            if (!string.IsNullOrWhiteSpace(custom))
            {
                if (Path.IsPathRooted(custom) && File.Exists(custom) && Path.GetExtension(custom).Equals(".exe", StringComparison.OrdinalIgnoreCase)) return custom;
                throw new InvalidOperationException("REVIT_CODEX_EXE phải trỏ đến đường dẫn đầy đủ của codex.exe.");
            }
            foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                try
                {
                    string candidate = Path.Combine(directory.Trim('"'), "codex.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException) { }
            }
            string bundled = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
            if (Directory.Exists(bundled))
            {
                string candidate = Directory.GetDirectories(bundled).Select(d => Path.Combine(d, "codex.exe"))
                    .Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                if (candidate != null) return candidate;
            }
            throw new InvalidOperationException("Chưa tìm thấy codex.exe. Cài Codex CLI, đăng nhập bằng codex login rồi khởi động lại Revit. "
                + "Có thể đặt REVIT_CODEX_EXE trỏ đến codex.exe.");
        }
    }
}
