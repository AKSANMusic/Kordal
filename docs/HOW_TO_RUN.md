# How to Run & Build Chordality

Chordality is a WinUI 3 desktop application requiring a Windows 10/11 environment. Because it leverages low-level WASAPI audio and Windows MIDI services, it cannot be run in Linux environments or WSL without specific hardware passthrough.

## Development (Visual Studio / JetBrains Rider)

1. Open `Chordality.slnx` in your IDE.
2. Ensure you have the **Windows App SDK** workloads installed.
3. Ensure the active build configuration is set to **Debug | x64** or **Debug | ARM64** (Avoid `Any CPU`).
4. Set `Chordality.App` as the Startup Project.
5. Click Run.

*Note: Debug builds may exhibit minor UI stutter compared to Release due to the heavy XAML debugging tools attached by the IDE.*

## Building the MSIX Release Package (Sideloading)

To experience the true zero-latency performance of the application, you must compile it in Release mode via MSIX. We have provided a PowerShell script to automate this.

### Step 1: Trust the Certificate
We use a self-signed certificate (`Chordality.pfx`) to package the application. Windows requires this certificate to be trusted on your local machine to allow sideloading.

Open an **Elevated (Administrator) PowerShell** window and run:
```powershell
cd path\to\Chordality\Chordality.App
Import-Certificate -FilePath .\Chordality.pfx -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

### Step 2: Build the Application
Open a standard PowerShell window in the root repository directory and execute the build script:
```powershell
.\build_release.ps1
```
This script cleans the solution, restores packages, completely disables AOT trimming (to protect MVVM bindings), and generates the final Native x64 MSIX bundle.

### Step 3: Install
Navigate to the output folder:
`Chordality.App\AppPackages\Chordality.App_1.0.0.0_x64_Test\`

Double-click the generated `.msix` file to prompt the standard Windows App Installer, or install it via PowerShell:
```powershell
Add-AppxPackage -Path .\Chordality.App_1.0.0.0_x64.msix
```