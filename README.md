# Field Assignment Generator Plugin for CLion (CLion Nova engine)


### About

CLion Nova plugin that generates field assignments for C++ structs and classes from code completion

Example:
```cpp
D3D12_RESOURCE_DESC desc;  
desc.

↓

desc.Dimension = ;  
desc.Alignment = ;  
desc.Width = ;  
desc.Height = ;  
desc.DepthOrArraySize = ;  
desc.MipLevels = ;  
desc.Format = ;  
desc.SampleDesc = ;  
desc.Layout = ;  
desc.Flags = ;
```

**⚠️ Alpha version**: Expect occasional bugs and unsupported language constructs while the plugin is being actively developed. If the plugin does behave incorrectly, I'm probably not aware of that, and it should be reported on GitHub Issues.

### Features

- Generates field assignments for public fields owned by the object itself
- Respects formatting settings like indent and spaces around operators
- Editable hotspots for quick value entry
- Preserves the member access operator (`.` / `->`) (Note that pointers are not supported yet)

### Planned

- Configuration
- Pointer support
- Customizable completion display order
- Generating assignments for inherited fields (optional)
- Generating assignments for fields that are private or protected but could be accessed from the completion context (optional)
- Comma separated generation (optional)

### Custom build configuration

This plugin uses a custom build configuration and internal CLion Nova APIs because an official plugin template for CLion is not yet available. To make it work, I made the following changes to the official [Plugin Template for ReSharper and Rider](https://github.com/jetbrains/resharper-rider-plugin):

- in gradle.build.kts:
    - Updated gradle to 9.0.0 - to support modern platform versions
    - Updated platform plugin to 2.17.0 - to support CLion 2026.1
    - Replaced rider with clion everywhere in build.gradle.kts
    - Updated kotlin and java versions to 21 - to support the updated platform plugin
    - Manually added riderRD artifact (from maven central), unpacked /lib/rd/rider-model.jar from it and republished it to the protocol's gradle.build.kts (needed for model generation, to be able to import classes like IdeRoot, use dependency injection etc. Otherwise the model wasn't even loaded. Generation worked though)
- in gradle/libs.versions.toml:
    - Updated kotlin version to kotlin 2.3.20 (to support CLion 2026.1)
    - Updated rdGen to 2026.1.0
- in C# project file:
    - Added references to JetBrains.ReSharper.Feature.Services and JetBrains.ReSharper.Feature.Services.Cpp (to create code completion and work with C++ PSI) (they are linked, but not referenced by default)

This plugin relies on internal CLion Nova APIs that are not yet officially documented and may change between IDE releases.
The project artifacts take approximately 7 GB in the Gradle cache directory.
