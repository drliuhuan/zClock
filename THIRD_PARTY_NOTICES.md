# Third-party notices

## QRCoder 1.6.0

- Project: https://github.com/codebude/QRCoder
- NuGet: https://www.nuget.org/packages/QRCoder/1.6.0
- License: MIT; full notice retained in `QRCoder-LICENSE.txt`.
- Restored from NuGet for building; dependency binaries are not committed.

## Microsoft PowerPoint / Windows

Microsoft PowerPoint is a separately installed proprietary application. This project uses Windows COM automation and a minimal ABI declaration to communicate with an existing installation. No Microsoft Office implementation, Office interop assembly, or Microsoft NuGet package is included or needed for compilation. Users must obtain and use Microsoft software under its own terms. GPL licensing of zClock does not relicense Microsoft products.

The earlier `Microsoft.Office.Interop.PowerPoint` NuGet dependency was removed before publishing because its package page did not provide a license.
