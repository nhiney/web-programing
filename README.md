# WebBanGIay (Shop Bán Giày)

Dự án Website bán giày được xây dựng trên nền tảng **ASP.NET MVC 5** (.NET Framework 4.7.2).

## Yêu cầu hệ thống

Để chạy được dự án này, máy tính của bạn cần cài đặt các công cụ sau (Môi trường Windows):

1.  **Visual Studio Build Tools** (hoặc Visual Studio 2019/2022) có kèm **MSBuild**.
2.  **IIS Express** (Thường đi kèm với Visual Studio).
3.  **Visual Studio Code**.
4.  **NuGet Command Line (nuget.exe)** (Để khôi phục các thư viện).

## Hướng dẫn chạy dự án với VS Code

Dự án này đã được cấu hình sẵn để chạy trên VS Code mà không cần mở Visual Studio đầy đủ.

### Bước 1: Khôi phục thư viện (NuGet Packages)

Do thư mục `packages/` đã bị loại bỏ khỏi git để giảm dung lượng, bạn cần khôi phục lại chúng trước khi chạy.

1.  Mở terminal tại thư mục gốc của dự án.
2.  Kiểm tra xem bạn đã có `nuget.exe` chưa. Nếu chưa, hãy tải về từ [dist.nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe) và để vào thư mục gốc.
3.  Chạy lệnh sau:
    ```powershell
    .\nuget.exe restore WebBanGIay.sln
    ```

### Bước 2: Build dự án

1.  Mở thư mục dự án bằng **VS Code**.
2.  Nhấn tổ hợp phím `Ctrl + Shift + B`.
3.  Chọn task **"build"**.
4.  Chờ quá trình build hoàn tất (Báo `Build succeeded` ở terminal).

### Bước 3: Chạy dự án (Debug)

1.  Nhấn phím `F5` (hoặc chuyển sang tab **Run and Debug** và chọn **"Debug (Chrome)"**).
2.  VS Code sẽ tự động:
    -   Khởi động **IIS Express** chạy ngầm ở cổng `8080`.
    -   Mở trình duyệt Google Chrome truy cập vào `http://localhost:8080`.

## Cấu trúc thư mục

-   `WebBanGIay/`: Mã nguồn chính của dự án (Controllers, Views, Models).
-   `packages/`: Chứa các thư viện (được tạo ra sau khi restore).
-   `.vscode/`: Chứa cấu hình chạy (`launch.json`) và build (`tasks.json`).

## Lưu ý

-   Nếu gặp lỗi **"Port 8080 is in use"**: Hãy kiểm tra dưới thanh taskbar (góc phải), tìm biểu tượng IIS Express, chuột phải và chọn Exit để tắt các process cũ.
-   Nếu gặp lỗi không tìm thấy `MSBuild`: Hãy kiểm tra file `.vscode/tasks.json` và cập nhật đường dẫn đến `MSBuild.exe` trên máy của bạn nếu khác biệt.
