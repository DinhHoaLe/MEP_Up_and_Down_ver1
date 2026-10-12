# MEP Up And Down — Revit 2024

Add-in WPF / .NET Framework 4.8 / x64 cho Duct, Pipe, Cable Tray và Conduit dạng cứng.

## Cách dùng

1. Chọn tab loại MEP, thao tác và offset dương (mm).
2. **Cut Up / Cut Down**: chọn góc 45° hoặc 90°, bấm Apply rồi chọn hai điểm trên **cùng một phần tử thẳng, nằm ngang**. Thứ tự chọn hai điểm không quan trọng. Đoạn giữa hai điểm được nâng/hạ; hai đầu ngoài giữ nguyên vị trí và kết nối. Với 45°, cần thêm khoảng trống bằng offset ở mỗi phía ngoài hai điểm chọn.
3. **Move Up / Move Down**: bấm Apply, chọn các phần tử cùng loại và fitting liên quan, rồi Finish. Góc không áp dụng. Revit có thể điều chỉnh phần tử nối liền; thao tác rollback nếu mất kết nối hiện có.
4. Esc hủy chọn và hiện lại cửa sổ. Có thể Undo thao tác đã hoàn tất trong Revit.

Cut sao chép phần tử gốc để giữ type, kích thước tiết diện và instance parameters, rồi tạo bốn elbow. Cần tải/configure family elbow tương thích với type, kích thước và góc. Cable Tray/Conduit phải có cấu hình fitting phù hợp. Phần tử có nhánh/tap, pinned hoặc trong group bị từ chối khi Cut; flex và đường cong không thuộc phạm vi hỗ trợ. Với insulation/lining và tham số phụ thuộc hình học, cần kiểm tra lại trên model thực tế.

Lỗi chọn, hình học, fitting hoặc transaction được báo và rollback. Không tự tải family hay thay routing preferences.

## Build và nạp vào Revit

Mở solution bằng Visual Studio có MSBuild và .NET Framework 4.8 targeting pack. Project tham chiếu RevitAPI/RevitAPIUI từ Revit 2024, target x64, không copy DLL API sang output.

Build Release tạo **bin/Release/MEP Up and Down.dll**. Manifest **MEP Up and Down.addin** trong thư mục project trỏ đến DLL Release tại checkout hiện tại. Copy manifest vào **%APPDATA%/Autodesk/Revit/Addins/2024/** rồi khởi động lại Revit; chạy command từ Add-Ins > External Tools. Nếu di chuyển project/DLL, cập nhật Assembly trong manifest. Nếu đã có manifest cho command này, thay manifest cũ để tránh đăng ký trùng.

## Symbol

**Resources/Symbols/** chứa 24 symbol PNG và vector XAML: 4 loại MEP × (Cut Up 45/90, Cut Down 45/90, Move Up, Move Down). **Symbol-Sheet.png** là bảng tổng hợp; **UI-Preview.png** là ảnh render layout.

Symbol live được vẽ bằng **UpDownTool/MEPSymbols.cs**, thay đổi theo tab, option, góc và offset. Nét đứt là tuyến gốc; màu là kết quả; mũi tên chỉ offset theo phương đứng. Preview là sơ đồ, không theo tỷ lệ model.

Giao diện dùng bảng màu, card, field và button styles từ mẫu **FamilyWizardWindow.xaml**: nền kem, nâu olive, card trắng bo góc và thanh tiêu đề tùy chỉnh. Cửa sổ có thể resize, minimize và maximize/restore; kích thước tối thiểu giữ preview đầy đủ. **UI-Preview-Minimum.png** là ảnh kiểm tra bố cục ở kích thước tối thiểu.

Icon **MEP_Up-and_Down_ver1** từ bảng tổng hợp nằm trong **Resources/Icons/MEP_Up-and_Down_ver1.png** (PNG nền trong suốt), được nhúng vào DLL và dùng cho Window.Icon, thanh tiêu đề và badge đầu cửa sổ. Không cần copy ảnh ngoài cùng DLL. Sơ đồ preview Cut/Move vẫn đổi theo lựa chọn thao tác.

Xuất lại: build Release rồi chạy Windows PowerShell ở chế độ STA với script **tools/Export-Symbols.ps1**. Script render symbol và layout thật với ảnh nhúng trong DLL, không gọi API Revit hoặc mở cửa sổ.

## Kiểm thử

Đã build Debug/Release và render toàn bộ 24 symbol cùng layout WPF. Chưa thực thi hình học/fitting trong một model Revit. Trước khi áp dụng vào model chính, thử trên bản sao với từng loại MEP: cut up/down 45/90, move tự do và tuyến nối fitting, chọn điểm ngược thứ tự, Esc, offset không hợp lệ, đoạn quá ngắn, type thiếu elbow, pinned/group và Undo. Kiểm tra kích thước, hướng tiết diện, bốn fitting, kết nối hai đầu và insulation/lining sau Cut.
