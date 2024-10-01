CREATE TABLE IF NOT EXISTS CS2S_Map (
    MapID varchar(128) PRIMARY KEY NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_Team (
    TeamID varchar(32) PRIMARY KEY,
    ELO int DEFAULT 1000,
    Name varchar(64) DEFAULT "Team"
);

CREATE TABLE IF NOT EXISTS CS2S_Player (
    PlayerID varchar(17) PRIMARY KEY NOT NULL,
    ELO int DEFAULT 1000,
    Kills int DEFAULT 0,
    Headshots int DEFAULT 0,
    Assists int DEFAULT 0,
    Deaths int DEFAULT 0,
    TotalDamage int DEFAULT 0,
    UtilityDamage int DEFAULT 0,
    RoundsKAST int DEFAULT 0,
    RoundsPlayed int DEFAULT 0,
    MatchesPlayed int DEFAULT 0
);

CREATE TABLE IF NOT EXISTS CS2S_Team_Players (
    TeamID varchar(32),
    PlayerID varchar(17),
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Match (
    MatchID int PRIMARY KEY AUTO_INCREMENT,
    MapID varchar(128),
    WinningTeamID varchar(32),
    LosingTeamID varchar(32),
    WinningTeamScore int,
    LosingTeamScore int,
    WinningSide int,
    DeltaELO int DEFAULT 0,
    FOREIGN KEY (MapID) REFERENCES CS2S_Map(MapID),
    FOREIGN KEY (WinningTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LosingTeamID) REFERENCES CS2S_Team(TeamID)
);

CREATE TABLE IF NOT EXISTS CS2S_Round (
    RoundID int PRIMARY KEY AUTO_INCREMENT,
    MatchID int,
    WinningTeamID varchar(32),
    LosingTeamID varchar(32),
    WinningSide int,
    RoundEndReason int,
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinningTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LosingTeamID) REFERENCES CS2S_Team(TeamID)
);

CREATE TABLE IF NOT EXISTS CS2S_Death (
    DeathID int PRIMARY KEY AUTO_INCREMENT,
    RoundID int,
    AttackerID varchar(17),
    AssisterID varchar(17),
    VictimID varchar(17),
    Weapon varchar(32),
    Hitgroup int,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (AssisterID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Hurt (
    HurtID int PRIMARY KEY AUTO_INCREMENT,
    AttackerID varchar(17),
    VictimID varchar(17),
    DamageAmount int,
    Weapon varchar(32),
    Hitgroup int,
    FOREIGN KEY (AttackerID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_PlayerInfo (
    PlayerID varchar(17) PRIMARY KEY NOT NULL,
    Username varchar(255) NOT NULL,
    AvatarS varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e.jpg",
    AvatarM varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_medium.jpg",
    AvatarL varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_full.jpg"
) ENGINE=MyISAM;

CREATE TABLE IF NOT EXISTS CS2S_Live (
	StaticID int PRIMARY KEY,
    TPlayers text, -- This can be a JSON object serialized as a string
    CTPlayers text, -- This can be a JSON object serialized as a string
    TScore int,
    CTScore int,
    BombStatus int,
    RoundTime int
) ENGINE=MyISAM;
