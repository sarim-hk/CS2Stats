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
    PlayerID varchar(17) NOT NULL,
    Username varchar(32) DEFAULT 'Anonymous',
    AvatarS varchar(256) DEFAULT 'https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e.jpg',
    AvatarM varchar(256) DEFAULT 'https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_medium.jpg',
    AvatarL varchar(256) DEFAULT 'https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/b5/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_full.jpg',
    ELO int DEFAULT 1000,
    PRIMARY KEY (PlayerID)
);

CREATE TABLE IF NOT EXISTS `Player_Match` (
    PlayerID varchar(17),
    MatchID int,
    PRIMARY KEY (PlayerID, MatchID),
    FOREIGN KEY (PlayerID) REFERENCES `Player`(PlayerID),
    FOREIGN KEY (MatchID) REFERENCES `Match`(MatchID)
);

CREATE TABLE IF NOT EXISTS `TeamPlayer` (
    TeamID int,
    PlayerID varchar(17),
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (PlayerID) REFERENCES `Player`(PlayerID),
    FOREIGN KEY (TeamID) REFERENCES `Team`(TeamID)
);

CREATE TABLE IF NOT EXISTS `PlayerStat` (
    PlayerStatID int AUTO_INCREMENT,
    PlayerID varchar(17),
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
    PlayerID varchar(17),
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
