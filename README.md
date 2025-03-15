# Lemonade Stand: Economic Simulation

A text-based economic simulation built in Unity that models markets, trade, and business operations to create an educational game about economics.

## Project Overview

This project simulates a virtual economy where players can create companies, trade goods, and participate in markets. The economic simulation includes features such as:

- Market simulation with supply and demand mechanics
- Company management with inventory systems
- Trading system with various price modifiers
- Multiple economic strategies and algorithms
- Goods with properties like rarity and expiration

The simulation serves as a foundation for learning economic principles in an interactive environment.

## Requirements

- Unity 2021.3.37f1 or compatible version
- .NET Standard 2.1 support
- Basic understanding of economic principles (for gameplay)

## Setup Instructions

1. Clone this repository
2. Open Unity Hub and add the project
3. Open the project using Unity 2021.3.37f1
4. Let Unity import and compile all assets
5. Open the main scene from `Assets/Scenes`

## Project Structure

The codebase is organized into modular components that handle different aspects of the economic simulation:

- **Actions**: Defines possible actions within the economic system
- **Company**: Contains company management logic and interfaces
- **Demand**: Implements demand strategies and consumption patterns
- **EntityStrategies**: Contains different business strategy implementations
- **ErrorHandling**: Custom error types and handling
- **FixedCosts**: Models fixed costs for businesses
- **Goods**: Models tradable items with properties
- **Inventory**: Manages storage of goods
- **Markets**: Implements market systems and transactions
- **PriceManagers**: Controls pricing algorithms
- **PriceModifiers**: Modifies prices based on various factors
- **Tests**: Contains unit tests for various components
- **The Economy**: Core singleton that ties everything together
- **Trade**: Handles trade orders and transactions
- **TradeProcessors**: Processes and prioritizes trades

## Core Concepts

### The Economy

The central manager that coordinates all economic activities, accessible through a singleton instance. It initializes markets, registers companies, and manages the overall economic state.

```csharp
// Example of accessing the economy
TheEconomy economy = TheEconomy.Instance;
```

### Markets

Places where goods are traded. Markets track supply, demand, and facilitate transactions between entities.

```csharp
// Example of getting the global market
Market globalMarket = TheEconomy.Instance.GetGlobalMarket();
```

### Goods

Items that can be bought, sold, and used in the economy. Goods have properties such as name, price bands, and rarity.

```csharp
// Creating a good
Good lemon = Good.CreateInstance("Lemon", priceBand, RarityEnum.Common);
lemon.ExpiresAfterPeriods = 1; // Lemons expire after one period
```

### Companies

Entities that participate in the economy by buying, selling, and producing goods.

```csharp
// Registering a company
Company myCompany = ScriptableObject.CreateInstance<Company>();
myCompany.Name = "Lemonade Stand Co.";
TheEconomy.Instance.RegisterCompany(myCompany);
```

## Running Tests

The project includes a comprehensive test suite to validate economic behaviors, you can run them on Unity Test Runner or Rider:

On Unity:
1. Open Unity Test Runner (Window > General > Test Runner)
2. Select EditMode tests
3. Click "Run All" to execute all tests

On Rider:
1. Test Explorer:
- Open the Unit Tests tool window (Alt+8 or View > Tool Windows > Unit Tests)
- Unity tests will appear in this window alongside any .NET tests

1. Running Tests:
- Right-click on test classes or methods in code
- Select "Run Unit Tests" or "Debug Unit Tests"
- Use the green "play" icon next to test classes or methods

1. Test Results:
- View results in the Unit Tests tool window
- Failed tests show detailed information about the failure

## Development Guidelines

- Add new features by extending existing interfaces when possible
- Write tests for new economic behaviors
- Keep simulation complexity manageable for educational purposes
- Document economic algorithms with comments and examples

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run all tests to ensure system integrity
5. Submit a pull request