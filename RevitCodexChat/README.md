# Codex · Revit AI (Revit 2024)

Add-in độc lập, .NET Framework 4.8 / x64. Khung chat WPF dock bên phải,
có Gửi, Dừng, Đọc phần tử đang chọn và Cuộc trò chuyện mới. Ctrl+Enter để gửi.
Không có Transaction, lệnh sửa model hoặc thực thi mã do AI tạo.

## Cài và mở

1. Đóng Revit 2024.
2. Chạy `powershell -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1` trong thư mục này.
3. Mở lại Revit, cho phép nạp add-in nếu Revit hỏi.
4. Add-Ins > Codex AI > Mở chat AI. Kéo bảng để thay đổi vị trí nếu muốn.

Phải nạp bằng manifest Application lúc Revit khởi động để đăng ký DockablePane;
không nạp như lệnh thông thường qua AddinManager.
Script chỉ cài manifest, DLL nằm tại bin/Release; không di chuyển/xóa thư mục sau khi cài.
Nếu chuyển thư mục, chạy lại Install.ps1. Để gỡ: đóng Revit rồi xóa riêng
`%APPDATA%\Autodesk\Revit\Addins\2024\RevitCodexChat.addin`.

## Kết nối Codex

Dùng `codex exec --json` với đăng nhập CLI hiện có. Add-in không đọc, sao chép hoặc
lưu token/API key. Máy cần Codex CLI hỗ trợ các cờ trong CodexClient.cs và đăng nhập
bằng `codex login`; kiểm tra bằng `codex login status`. Tự tìm codex.exe trong PATH
hoặc bản đi kèm Codex desktop dưới LocalAppData/OpenAI/Codex/bin. Nếu cần, đặt biến
môi trường REVIT_CODEX_EXE thành đường dẫn đầy đủ đến codex.exe rồi mở lại Revit.
Không dùng mô hình giả lập. Kết nối cần mạng và quyền/hạn mức Codex của tài khoản.

Mỗi lần gửi là một tiến trình ephemeral, gửi lại tối đa 6 lượt hội thoại thành công
(24.000 ký tự) và snapshot hiện tại. Không tiếp tục chat hiện có của ứng dụng Codex.
Không kế thừa config người dùng: tắt shell, unified exec, apps, plugins, hooks,
browser và computer use; bật sandbox read-only. Không truyền đường dẫn model.
Nút Dừng kết thúc tiến trình Codex của yêu cầu này; tự dừng sau 4 phút.

## Dữ liệu lựa chọn

1. Chọn phần tử trong model.
2. Bấm Đọc phần tử đang chọn. Dữ liệu được đọc qua ExternalEvent trên luồng API Revit.
3. Mở Xem dữ liệu sẽ gửi để kiểm tra, sau đó nhập câu hỏi và bấm Gửi.

Snapshot gồm tên tài liệu (không gồm đường dẫn), ID, tên, loại, level, pinned,
trục đoạn thẳng và chiều dài mm, connector và ID nối, tối đa 40 thông số/phần tử,
tối đa 20 phần tử. Đây là snapshot có thời gian đọc, không tự cập nhật khi đổi lựa
chọn hoặc đổi tài liệu; hãy đọc lại. Dữ liệu chỉ gửi đến Codex khi bấm Gửi.
Cuộc trò chuyện mới xóa hội thoại và snapshot khỏi giao diện/bộ nhớ add-in.
Phần tử trong linked model không được duyệt sâu. Không truy vấn toàn bộ mạng MEP.
AI chỉ nhận văn bản, không thể thao tác model qua cầu nối này.

## Build và kiểm tra

Build RevitCodexChat.csproj bằng Visual Studio/MSBuild với .NET 4.8 targeting pack;
RevitPath mặc định `C:\Program Files\Autodesk\Revit 2024`.
Kiểm tra thực tế trong Revit: mở/đóng pane, resize, đọc không có selection,
đọc Pipe/Duct/Fitting, đổi lựa chọn và đọc lại, gửi tiếng Việt, hỏi tiếp, dừng,
mất mạng/chưa đăng nhập, đóng Revit trong khi AI trả lời. Kiểm tra Undo/model không
phát sinh thay đổi. Build và preview WPF không thay thế kiểm thử trong Revit.

Tài liệu: https://developers.openai.com/codex/noninteractive/
và Autodesk Revit 2024 API Developer Guide — Dockable Dialog Panes.

## Kết quả xác minh 2026-10-14

- Build Release thành công với Revit API 2024.
- Render trực tiếp ChatPane đã biên dịch: Preview.png (dữ liệu minh họa).
- Đọc thất bại xóa snapshot cũ: đạt.
- Gửi yêu cầu thử nghiệm qua Codex CLI bằng đăng nhập ChatGPT: nhận câu trả lời tiếng Việt.
- Hủy yêu cầu đang chạy: Task chuyển trạng thái canceled và tiến trình kết thúc.
- Đã cài manifest và xác nhận DLL tồn tại ở đường dẫn trong manifest.
- Chưa chạy DockablePane/ExternalEvent trong phiên Revit với model thật.

Chạy lại kiểm tra bằng Windows PowerShell 5.1 x64:
`powershell -NoProfile -STA -ExecutionPolicy Bypass -File .\Verify.ps1 -Live`.
Không thêm -Live nếu chỉ muốn render và kiểm tra UI mà không gọi Codex.
