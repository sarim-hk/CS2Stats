CREATE TABLE IF NOT EXISTS CS2S_Map (
    MapID varchar(128) PRIMARY KEY NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_PlayerInfo (
    PlayerID varchar(17) PRIMARY KEY NOT NULL,
    Username varchar(255) NOT NULL,
    AvatarS varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e.jpg" NOT NULL,
    AvatarM varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_medium.jpg" NOT NULL,
    AvatarL varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_full.jpg" NOT NULL
) ENGINE=MyISAM;

CREATE TABLE IF NOT EXISTS CS2S_Player (
    PlayerID varchar(17) PRIMARY KEY NOT NULL,
    ELO int DEFAULT 1000 NOT NULL,
    Kills int DEFAULT 0 NOT NULL,
    Headshots int DEFAULT 0 NOT NULL,
    Assists int DEFAULT 0 NOT NULL,
    Deaths int DEFAULT 0 NOT NULL,
	Damage int DEFAULT 0 NOT NULL,
    UtilityDamage int DEFAULT 0 NOT NULL,
    EnemiesFlashed int DEFAULT 0 NOT NULL,
    RoundsKAST int DEFAULT 0 NOT NULL,
    RoundsPlayed int DEFAULT 0 NOT NULL,
    MatchesPlayed int DEFAULT 0 NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_Match (
    MatchID int PRIMARY KEY NOT NULL,
    MapID varchar(128) NOT NULL,
    StartTick int NOT NULL,
    EndTick int NULL,
    MatchDate datetime DEFAULT CURRENT_TIMESTAMP NOT NULL,
    FOREIGN KEY (MapID) REFERENCES CS2S_Map(MapID)
);

CREATE TABLE IF NOT EXISTS CS2S_Team (
    TeamID varchar(32) PRIMARY KEY,
	Size int NOT NULL,
    ELO int DEFAULT 1000 NOT NULL,
    Name varchar(64) DEFAULT "Team" NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_TeamResult (
    TeamID varchar(32) NOT NULL,
	MatchID int NOT NULL,
	Score int NULL,
    Result ENUM("Win", "Loss", "Tie") NULL,
    DeltaELO int DEFAULT 0 NOT NULL,
    PRIMARY KEY (TeamID, MatchID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID)
);

CREATE TABLE IF NOT EXISTS CS2S_Round (
    RoundID int PRIMARY KEY NOT NULL,
    MatchID int NOT NULL,
    WinningTeamID varchar(32) NULL,
    LosingTeamID varchar(32) NULL,
    WinningSide int NULL,
    RoundEndReason int NULL,
    StartTick int NOT NULL,
    EndTick int NULL,
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinningTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LosingTeamID) REFERENCES CS2S_Team(TeamID)
);

CREATE TABLE IF NOT EXISTS CS2S_Death (
    DeathID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    AttackerID varchar(17) NULL,
    AssisterID varchar(17) NULL,
    VictimID varchar(17) NOT NULL,
    Weapon varchar(32) NOT NULL,
    Hitgroup int NOT NULL,
    RoundTick int NOT NULL,
    OpeningDeath bool NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (AssisterID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Hurt (
    HurtID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    AttackerID varchar(17) NULL,
    VictimID varchar(17) NOT NULL,
    DamageAmount int NOT NULL,
    Weapon varchar(32) NOT NULL,
    Hitgroup int NOT NULL,
    RoundTick int NOT NULL,
	FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_KAST (
    KASTID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    PlayerID varchar(17) NOT NULL,
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Blind (
    BlindID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    ThrowerID varchar(17) NOT NULL,
	BlindedID varchar(17) NOT NULL,
    Duration float NOT NULL,
    TeamFlash bool NOT NULL,
    RoundTick int NOT NULL,
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (ThrowerID) REFERENCES CS2S_Player(PlayerID),
	FOREIGN KEY (BlindedID) REFERENCES CS2S_Player(PlayerID)
);

/*
CREATE TABLE IF NOT EXISTS CS2S_PlayerRoundStat (
   PlayerRoundStatID int PRIMARY KEY AUTO_INCREMENT,
   RoundID int,
   MatchID int,
   PlayerID varchar(17),
   Kills int DEFAULT 0,
   Assists int DEFAULT 0,
   Deaths int DEFAULT 0,
   Damage int DEFAULT 0,
   UtilityDamage int DEFAULT 0,
   FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
   FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
   FOREIGN KEY (PlayerID) REFERENCES CS2S_Player(PlayerID)
);
*/

CREATE TABLE IF NOT EXISTS CS2S_Live (
	StaticID int PRIMARY KEY NOT NULL,
    TPlayers text NULL, 
    CTPlayers text NULL,
    TScore int NULL,
    CTScore int NULL,
    BombStatus int NULL,
    RoundTick int NULL
) ENGINE=MyISAM;

CREATE TABLE IF NOT EXISTS CS2S_Team_Players (
    TeamID varchar(32) NOT NULL,
    PlayerID varchar(17) NOT NULL,
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_Player(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Player_Matches (
    PlayerID varchar(17) NOT NULL,
	MatchID int NOT NULL,
    PRIMARY KEY (PlayerID, MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID)
);

