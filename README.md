# Lantern.Discv5 
Lantern.Discv5 is a C# implementation of the Ethereum Discovery Protocol Version 5 (Discv5) which provides a robust and efficient peer-to-peer network communication. This project aims to offer a reliable, extensible, and compatible solution for modern applications.

The Discv5 specification is available at the [Ethereum devp2p repository](https://github.com/ethereum/devp2p/blob/master/discv5/discv5.md).

By providing this implementation, we enable developers to integrate and utilize Discv5 for Ethereum-based projects and any application requiring peer-to-peer communication within the .NET ecosystem.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Features
The following features have been implemented:
- Compliance with relevant parts of Discv5's [wire](https://github.com/ethereum/devp2p/blob/master/discv5/discv5-wire.md) and [theory](https://github.com/ethereum/devp2p/blob/master/discv5/discv5-theory.md) specifications
- Support for RLP (Recursive Length Prefix) serialization and deserialization
- Support for using ENR (Ethereum Node Record) with extensibility

*Note: This implementation does not support topic advertisement because it will be removed from the specification in the upcoming Discovery Protocol v5.2.*

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

## Usage

TODO

## Contributing

We welcome contributions to the Lantern.Discv5 project. To get involved, please read our [Contributing Guidelines](CONTRIBUTING.md) for the process for submitting pull requests to us.

## License
This project is licensed under the [MIT License](https://github.com/Pier-Two/Lantern.Discv5/blob/main/LICENSE).