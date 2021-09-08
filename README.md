[![Tilia logo][Tilia-Image]](#)

> ### Tilia Unity UI Unity Editor 2017 +
> Unity UI with UI Pointers.

[![Release][Version-Release]][Releases]
[![License][License-Badge]][License]
[![Backlog][Backlog-Badge]][Backlog]

## Introduction

Support for Unity UI. Both New and Old input systems

> **Requires** Unity Editor 2017.4 +


## Important usage

    - Place VRTK4_UICanvas on each canvas
    - Mark your canvas graphic raycaster with Blocking Mask (never leave as none)!
    - In addition to the VRTK4_UI_ToPointer add VRTK4_UIPointer add to the same gameobject
    - To Ignore Player Objects - Use Component VRTK4_Player Object
    - Also, if you are using Pseudobody - add pointers to the IgnoredGameObjectList

## Getting Started

Please refer to the [installation] guide to install this package.

## Documentation

Please refer to the [How To Guides] for usage of this package.

Further documentation can be found within the [Documentation] directory and at https://academy.vrtk.io

## Contributing

Please refer to the Extend Reality [Contributing guidelines] and the [project coding conventions].

## Code of Conduct

Please refer to the Extend Reality [Code of Conduct].

## License

Code released under the [MIT License][License].

[License-Badge]: https://img.shields.io/github/license/ExtendRealityLtd/Tilia.{scope}.{feature}.{platform?}.svg
[Version-Release]: https://img.shields.io/github/release/ExtendRealityLtd/Tilia.{scope}.{feature}.{platform?}.svg
[project coding conventions]: https://github.com/ExtendRealityLtd/.github/blob/master/CONVENTIONS/{project_type}

[Tilia-Image]: https://user-images.githubusercontent.com/1029673/67681496-5bf10700-f985-11e9-9413-e61801b6eab5.png
[License]: LICENSE.md
[Documentation]: Documentation/
[How To Guides]: Documentation/HowToGuides/
[Installation]: Documentation/HowToGuides/Installation/README.md
[Backlog]: http://tracker.vrtk.io
[Backlog-Badge]: https://img.shields.io/badge/project-backlog-78bdf2.svg
[Releases]: ../../releases
[Contributing guidelines]: https://github.com/ExtendRealityLtd/.github/blob/master/CONTRIBUTING.md
[Code of Conduct]: https://github.com/ExtendRealityLtd/.github/blob/master/CODE_OF_CONDUCT.md
