# Lantern.Discv5 
Lantern.Discv5 is a C# implementation of the Ethereum Discovery Protocol Version 5.1 (Discv5) which provides a robust and efficient peer-to-peer network communication. This project aims to offer a reliable, extensible, and compatible solution for modern applications.

The Discv5 specification is available at the [Ethereum devp2p repository](https://github.com/ethereum/devp2p/blob/master/discv5/discv5.md).

By providing this implementation, developers can integrate and utilize Discv5 for Ethereum-based projects and any application requiring peer-to-peer communication within the .NET ecosystem.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Usage](#quick-usage)
- [Contributing](#contributing)
- [License](#license)

## Features
The following features have been implemented:
- Compliance with relevant parts of Discv5's [wire](https://github.com/ethereum/devp2p/blob/master/discv5/discv5-wire.md) and [theory](https://github.com/ethereum/devp2p/blob/master/discv5/discv5-theory.md) specifications
- Support for RLP (Recursive Length Prefix) serialization and deserialization
- Support for using ENR (Ethereum Node Record) with extensibility

*Note: This implementation does not support topic advertisement because it will be removed from the specification in the upcoming [Discovery Protocol v5.2](https://github.com/ethereum/devp2p/issues/226).*

## Installation

*Note: These instructions assume you are familiar with the .NET Core development environment. If not, please refer to the [official documentation](https://docs.microsoft.com/en-us/dotnet/core/introduction) to get started.*

1. Install [.NET Core SDK](https://docs.microsoft.com/en-us/dotnet/core/install/) on your system if you haven't already.

2. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/lantern.discv5.git
   ```

3. Change to the `lantern.discv5` directory:

   ```bash
   cd lantern.discv5
   ```

4. Build the project:

   ```bash
   dotnet build
   ```
5. Execute tests:
   ```bash
   dotnet test
   ```

## Quick Usage

This library can used in any C# project by using the following import statement: 
```
using Lantern.Discv5.WireProtocol;
```
Once this statement is added, the protocol can be initialised by providing any number of bootstrap ENRS:
```
Discv5Protocol discv5 = Discv5Builder.CreateDefault(bootstrapEnrs);
```
Here, `bootstrapEnrs` is  an array of strings. As an example, the following bootstrap ENRs can be used for initialising:
```
var bootstrapEnrs = new[]
{
"enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg",
"enr:-KG4QOtcP9X1FbIMOe17QNMKqDxCpm14jcX5tiOE4_TyMrFqbmhPZHK_ZPG2Gxb1GE2xdtodOfx9-cgvNtxnRyHEmC0ghGV0aDKQ9aX9QgAAAAD__________4JpZIJ2NIJpcIQDE8KdiXNlY3AyNTZrMaEDhpehBDbZjM_L9ek699Y7vhUJ-eAdMyQW_Fil522Y0fODdGNwgiMog3VkcIIjKA"
};
```

For a more detailed overview, we recommend checking our [Usage](USAGE.md) guide which describes the available functionalities and configuration options.

## Contributing

We welcome contributions to the Lantern.Discv5 project. To get involved, please read our [Contributing Guidelines](CONTRIBUTING.md) for the process for submitting pull requests to us.

## License
This project is licensed under the [MIT License](https://github.com/Pier-Two/Lantern.Discv5/blob/main/LICENSE).
