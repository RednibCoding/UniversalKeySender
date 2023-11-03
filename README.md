# Universal Key Sender

Universal Key Sender is a tool that allows users to automate keypress sequences to a specified window on Windows operating systems. Users can define a sequence of delays and keycodes in a `keys.txt` file, which the tool will then send to a window with the provided title.

## Features

- **Window Title Targeting**: Send keys to a specific window based on the window title.
- **Customizable Delays**: Define the delay in milliseconds before each keypress.
- **Repeat Sequences**: Specify a final delay after which the sequence of keypresses will repeat.

## How to build
Run the following .NET command:
```sh
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## How to Use

#### 1. Clone the repository or download the latest release.
#### 2. Create a `keys.txt` file in the same directory as the `UniversalKeySender` executable with the following format:
```
MyTargetWindow ; The title of the target window

; Following the delay and key pairs below each other:
200 ; Delay of 200 milliseconds
9 ; Virtual key code for Tab key
200 ; Another delay of 200 milliseconds
31 ; Virtual key code for '1' key
; The final delay before the sequence should start again:
30000 ; Delay of 30000 milliseconds (30 seconds)
```

Replace `MyTargetWindow` with the title of the window you want to target, and specify your desired keycodes and delays.

#### 3. Run UniversalKeySender.exe

This will generate a self-contained executable that you can run on any Windows x64 machine without needing to install additional dependencies.

## Keycode Reference
You will need to refer to the virtual key codes which can be found in the [Microsoft Documentation](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes).

## Notes
- The tool will continuously run the keypress sequence until the program is manually stopped.
- Ensure that the window title is exactly as it appears on the window you wish to target.
- The final delay indicates how long the tool will wait before restarting the sequence.

## License
Permissive MIT License

---

Made with ❤️ by [RednibCoding](https://github.com/RednibCoding)
