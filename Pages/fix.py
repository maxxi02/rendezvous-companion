with open('Pages/PrinterSettingsPage.xaml.cs', 'rb') as f:
    raw = f.read()

old = b'#endif\r\n\n    // '
new = b'#endif\r\n        }\r\n    }\r\n\n    // '

if old in raw:
    fixed = raw.replace(old, new, 1)
    with open('Pages/PrinterSettingsPage.xaml.cs', 'wb') as f:
        f.write(fixed)
    print('FIXED')
else:
    print('PATTERN NOT FOUND')
