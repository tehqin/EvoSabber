# EvoSabber

This project is the experimental environment for the paper [Mapping Hearthstone Deck Spaces through MAP-Elites with Sliding Boundaries](https://arxiv.org/abs/1904.10656).

The goal of the project is to automatically generate decks for the digital card game Hearthstone. The project is frozen in time to allow for easy replication of the experiments from the paper. However, the experiments are configurable through TOML files and it is fairly easy to modify to use new card sets or search different gameplay properties.

The project was designed to run through mono on a Linux-based system. This was mostly done because our HPC only had mono installed and it wasn't possible to install dotnet quickly enough for our experiments. SabberStone has moved to be entirely dotnet. To make this legacy system possible, we've included the SabberStone dll compiled for mono. Therefore, it should be easy to install and run this project purely from mono. Under development is a dotnet based project that will be released soon and compatible with the new SabberStone

## Installation

First install [mono](https://www.mono-project.com/download/stable/) and msbuild using the method that is compatible with your system.

```
sudo apt-get install msbuild
```

To run experiments you need to compile "DeckEvaluator" which is a stand alone program for evaluating Hearthstone decks and "MapSabber" which is a distributed MAP-Elites implementation. To compile a release version of the program run the following build script in each projects directory.

```
msbuild /p:Configuration=Release
```

From this point you should have enough compiled to run the experiments from the paper!

## Running Experiments

Move to the `TestBed/MapElites` directory. First we need to setup folders for logging experiments and communicating between the `DeckEvaluator` program and the `MapSabber` program. To do this run the following code.

```
mkdir active boxes logs
```

The `active` folder is where each program sends initialization information between each other. The `boxes` folder is where messages are sent between MapSabber and DeckEvaluator. The `logs` folder keeps csv files of individuals and map information throughout the experiment. Each of the folders should be empty when the experiment starts. 

Now we are ready to start a search. You can run the following command.

```
mono ../../MapSabber/MapSabber/bin/Release/MapSabber.exe config/testWarlock.tml
```

This will start an experiment using MAP-Elites where the first parameter is the config file of the experiment. But to start playing games we need to start a node.

```
mono ../../DeckEvaluator/DeckEvaluator/bin/Release/DeckEvaluator.exe 1
```

This starts a deck evalutation node. The first number is the ID of the node being run. Each deck evaluation node needs a separate ID and must be started after the search node boots.

The experiments were designed to be run with 499 `DeckEvaluator` nodes and 1 `MapSabber` node. Included in TestBed are two shell scripts for starting nodes using GridEngine.

## Config Files
Each configuration file allows for configuration of which card sets are allowed, the number of games to play using a specified strategy, the opponent decks and strategies, and configuration information for MAP-Elites. Below is an example config file for running 200 games per deck against a suite of starter decks. The search is exploring control decks for the Hunter class and diversifying over the Mana curve of each deck.

```
[Evaluation]
OpponentDeckSuite = "resources/decks/suites/starterMeta.tml"
DeckPools = ["resources/decks/pools/starterDecks.tml"]

[[Evaluation.PlayerStrategies]]
NumGames = 200
Strategy = "Control"

[Deckspace]
HeroClass = "hunter"
CardSets = ["CORE", "EXPERT1"]

[Search]
InitialPopulation = 100
NumToEvaluate = 10000

[Map]
Type = "SlidingFeature"
RemapFrequency = 100
StartSize = 2
EndSize = 20

[[Map.Features]]
Name = "DeckManaSum"

[[Map.Features]]
Name = "DeckManaVariance"
```
