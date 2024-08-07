CREATE TABLE IF NOT EXISTS `Team` (
    TeamID int AUTO_INCREMENT,
    PRIMARY KEY (TeamID)
);

CREATE TABLE IF NOT EXISTS `Match` (
    MatchID int AUTO_INCREMENT,
    Map varchar(128),
    TeamTID int,
    TeamCTID int,
    TeamTScore int,
    TeamCTScore int,
    MatchDate datetime DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (MatchID),
    FOREIGN KEY (TeamTID) REFERENCES `Team`(TeamID),
    FOREIGN KEY (TeamCTID) REFERENCES `Team`(TeamID)
);

CREATE TABLE IF NOT EXISTS `Player` (
    PlayerID bigint UNSIGNED,
    ELO int DEFAULT 1000,
    PRIMARY KEY (PlayerID)
);

CREATE TABLE IF NOT EXISTS `Player_Match` (
    PlayerID bigint UNSIGNED,
    MatchID int,
    PRIMARY KEY (PlayerID, MatchID),
    FOREIGN KEY (PlayerID) REFERENCES `Player`(PlayerID),
    FOREIGN KEY (MatchID) REFERENCES `Match`(MatchID)
);

CREATE TABLE IF NOT EXISTS `TeamPlayer` (
    TeamID int,
    PlayerID bigint UNSIGNED,
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (PlayerID) REFERENCES `Player`(PlayerID),
    FOREIGN KEY (TeamID) REFERENCES `Team`(TeamID)
);

CREATE TABLE IF NOT EXISTS `PlayerStat` (
    PlayerStatID int AUTO_INCREMENT,
    PlayerID bigint UNSIGNED,
    Kills int,
    Headshots int,
    Assists int,
    Deaths int,
    TotalDamage int,
    UtilityDamage int,
    RoundsPlayed int,
    PRIMARY KEY (PlayerStatID),
    FOREIGN KEY (PlayerID) REFERENCES `Player`(PlayerID)
);

CREATE TABLE IF NOT EXISTS `Player_PlayerStat` (
    PlayerID bigint UNSIGNED,
    PlayerStatID int,
    PRIMARY KEY (PlayerID, PlayerStatID),
    FOREIGN KEY (PlayerID) REFERENCES `Player`(PlayerID),
    FOREIGN KEY (PlayerStatID) REFERENCES `PlayerStat`(PlayerStatID)
);

CREATE TABLE IF NOT EXISTS `Match_PlayerStat` (
    MatchID int,
    PlayerStatID int,
    PRIMARY KEY (MatchID, PlayerStatID),
    FOREIGN KEY (MatchID) REFERENCES `Match`(MatchID),
    FOREIGN KEY (PlayerStatID) REFERENCES `PlayerStat`(PlayerStatID)
);
