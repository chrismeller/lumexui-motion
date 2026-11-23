## 🚧 Notice 🚧

This project is in a very raw and experimental state. It may contain bugs, missing features, and breaking changes as development progresses.

This project was primarily created to support [the main LumexUI project](https://github.com/LumexUI/lumexui), and functionality is added only as needed to fulfill its requirements. As a result, it may lack certain features or general-purpose usability.

If you choose to explore or contribute, please be aware that stability is not guaranteed, and updates may be infrequent or focused on specific needs.

## About this fork

This is a maintained fork of [LumexUI/lumexui-motion](https://github.com/LumexUI/lumexui-motion), which is no longer developed. The main change: the `motion` library is bundled into the package's static assets instead of being imported from a CDN at runtime, so apps work offline and under a strict Content-Security-Policy.

The JavaScript lives in `src/Scripts/`. `dotnet build` bundles it into `src/wwwroot/motion.js` with rollup (not committed), so building needs Node and pnpm: `mise install` sets them up.

## Restoring the package

The fork is published to GitHub Packages, not nuget.org. The upstream packages are still on nuget.org under the same IDs (`LumexUI.Motion` 0.0.4 and `LumexUI` 2.4.0, both archived), so a project that only adds the feed can restore upstream's package instead of the fork's. Package source mapping prevents that.

### 1. Add a `nuget.config` next to your solution

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="chrismeller" value="https://nuget.pkg.github.com/chrismeller/index.json" />
  </packageSources>
  <packageSourceMapping>
    <!-- The forks only ever come from the chrismeller feed. -->
    <packageSource key="chrismeller">
      <package pattern="LumexUI" />
      <package pattern="LumexUI.Motion" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

The more specific pattern wins, so `LumexUI` and `LumexUI.Motion` only restore from `chrismeller`, and everything else from nuget.org.

### 2. Get a token

GitHub Packages needs a token even for public packages. Use a **personal access token (classic) with the `read:packages` scope**. GitHub Packages doesn't accept fine-grained tokens ([GitHub docs](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages)).

### 3. Give NuGet the credentials

The credentials belong to the source named `chrismeller` in the `nuget.config` above. Keep them out of that file.

**Locally:** store them in your user-level NuGet config. That file is `~/.nuget/NuGet/NuGet.Config` on macOS and Linux, and `%APPDATA%\NuGet\NuGet.Config` on Windows:

```shell
dotnet nuget add source "https://nuget.pkg.github.com/chrismeller/index.json" \
  --name chrismeller --username <github-user> --password <token> \
  --store-password-in-clear-text --configfile ~/.nuget/NuGet/NuGet.Config
```

NuGet only encrypts stored passwords on Windows, which is why `--store-password-in-clear-text` is needed on macOS and Linux. On Windows you can leave it out.

**In CI:** set `NuGetPackageSourceCredentials_chrismeller`, which NuGet reads for the `chrismeller` source. In GitHub Actions:

```yaml
env:
  NuGetPackageSourceCredentials_chrismeller: Username=${{ github.actor }};Password=${{ secrets.GITHUB_TOKEN }}
```

Use `secrets.GITHUB_TOKEN` with `permissions: packages: read`, and only if the package's settings give your repository Actions access ("Manage Actions access" on the package page). Otherwise, store a classic token with `read:packages` as a secret and use that.

## Installation

0. Add the package: `dotnet add package LumexUI.Motion` (after [restoring is set up](#restoring-the-package)).

1. Register the service in the DI:
   
```csharp
// Program.cs

using LumexUI.Motion.Extensions;

builder.Services.AddLumexMotion();
```

2. Globally add the necessary usings:

```razor
@* _Imports.razor *@

@using LumexUI.Motion
@using LumexUI.Motion.Types
```

## API

Since this project is a Blazor wrapper, I mimic the original API, but it’s not fully implemented yet. 
For more details, refer to the [original Motion React documentation](https://motion.dev/docs/react-animation).

```csharp
public record MotionProps
{
    // Animations that are triggered on after render.
    public object? Enter { get; init; }

    // Animations that are triggered before component is removed from the render tree.
    public object? Exit { get; init; }

    // Transition settings for all animations.
    public object? Transition { get; init; }
};
```

The properties are of type `object` purely for simplicity, as they are later serialized into JavaScript objects.
Additionally, this makes it easier to follow the Motion's usage examples.

## Examples

The Motion library is one of the most powerful animation libraries available, allowing you to create almost any animation you want.
Check out the full list of Motion vanilla JavaScript examples [here](https://examples.motion.dev/js).

Below are some of the simplest animation examples to give you an idea of how it works in this library.

#### Simple Fade-In Animation

```razor
@* A component that wraps content for animation. *@
<Motion Enter="@_motionProps.Enter">
    <h1>Hello, world!</h1>
</Motion>

@code {
    private MotionProps _motionProps = new MotionProps
    {
        Enter = new
        {
            Opacity = new float[] { 0, 1 } // Animate opacity from 0 to 1.
        }
    };
}
```

https://github.com/user-attachments/assets/7b86932e-7e5c-422e-959d-a091f11ee4ef

#### Simple Fade-Out Animation

```razor
<button @onclick="@(() => _isVisible = !_isVisible)">
    @(_isVisible ? "Hide" : "Show")
</button>

@* A component that detects when its direct children are removed from the render tree. *@
<AnimatePresence>
    @if( _isVisible )
    {
        <Motion Exit="@_motionProps.Exit">
            <h1>Hello, world!</h1>
        </Motion>
    }
</AnimatePresence>

@code {
    private bool _isVisible = false;

    private MotionProps _motionProps = new MotionProps
    {
        Exit = new
        {
            Opacity = 0 // Animate opacity from initial (1) to 0.
        }
    };
}
```

https://github.com/user-attachments/assets/ee0139fb-0e83-45b4-a095-907c0b2947c8

#### Simple Fade-In-Out Animation

```razor
<button @onclick="@(() => _isVisible = !_isVisible)">
    @(_isVisible ? "Hide" : "Show")
</button>

<AnimatePresence>
    @if( _isVisible )
    {
        <Motion Enter="@_motionProps.Enter" Exit="@_motionProps.Exit">
            <h1>Hello, world!</h1>
        </Motion>
    }
</AnimatePresence>

@code {
    private bool _isVisible = false;

    private MotionProps _motionProps = new MotionProps
    {
        Enter = new
        {
            Opacity = new float[] { 0, 1 } // Animate opacity from 0 to 1.
        },

        Exit = new
        {
            Opacity = 0 // Animate opacity from current (1) to 0.
        }
    };
}
```

https://github.com/user-attachments/assets/e98c272d-977e-4ad9-b919-630db6d84016

Ultimately, the component is actually removed from the DOM.
