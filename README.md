[![CS2Stats](https://i.imgur.com/LUTaSX2.png)](https://mapink.sarim.uk)

CS2Stats is a Counter-Strike 2 plugin for running matches with in-depth stat tracking.

## Feature Highlights:
* Automatic demo recording
* Built in ELO system to rank players
* In-depth stat tracking, per match, per round:
  *  ELO
  *  Kills, Assists, Deaths
  *  Utility Damage, Total Damage
  *  Enemies Flashed, Grenades Thrown
  *  Clutch Attempts and Clutches Won
  *  KAST
  *  Rounds Played and Matches Played

## Donation
[!["Paypal"](https://i.imgur.com/7igL5rh.png)](https://paypal.me/SHKTV)‎ ‎ ‎ ‎ [!["Steam Trade Link"](https://i.imgur.com/33ijkjI.png)](https://steamcommunity.com/tradeoffer/new/?partner=317935564&token=ZBiuL2Ge)

## Prerequisites
* [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) installed on the game server
* MySQL Server
* Web server for hosting API

## Installation
* Install the [latest release build](https://github.com/sarim-hk/CS2Stats/releases)
* Load the plugin once to generate the config at `counterstrikesharp\configs\plugins\CS2Stats`
* Edit the config file to include:
  * MySQL Server Details
  * Steam API key
  * Demo recording
* Restart the server, or reload the plugin
* Done!
