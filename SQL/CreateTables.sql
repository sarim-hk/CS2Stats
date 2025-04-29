-- This should probably just be stored in CS2S_Match
-- since we don't store any additional info on maps, or reference this table anywhere else
CREATE TABLE IF NOT EXISTS CS2S_Map (
    MapID varchar(128) PRIMARY KEY NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_PlayerInfo (
    PlayerID bigint unsigned PRIMARY KEY NOT NULL,
	ELO int DEFAULT 1000 NOT NULL,
    Username varchar(64) NOT NULL,
    AvatarHash varchar(64) NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_Match (
    MatchID int unsigned PRIMARY KEY NOT NULL,
    MapID varchar(128) NOT NULL,
    StartTick int NOT NULL,
    EndTick int NOT NULL,
    MatchDate datetime DEFAULT CURRENT_TIMESTAMP NOT NULL,
    FOREIGN KEY (MapID) REFERENCES CS2S_Map(MapID)
);

CREATE TABLE IF NOT EXISTS CS2S_Team (
    TeamID varchar(32) PRIMARY KEY,
	Size int unsigned NOT NULL,
    ELO int DEFAULT 1000 NOT NULL,
    Name varchar(64) DEFAULT "Team" NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_TeamResult (
    TeamID varchar(32) NOT NULL,
	MatchID int unsigned NOT NULL,
	Score smallint unsigned NOT NULL,
    Result ENUM("Win", "Loss", "Tie") NOT NULL,
    
     -- t = 2, ct = 3
    Side tinyint unsigned NOT NULL,

    DeltaELO int DEFAULT 0 NOT NULL,
    PRIMARY KEY (TeamID, MatchID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID)
);

CREATE TABLE IF NOT EXISTS CS2S_Round (
    RoundID int unsigned PRIMARY KEY,
    MatchID int unsigned NOT NULL,
    WinnerTeamID varchar(32) NOT NULL,
    LoserTeamID varchar(32) NOT NULL,

    -- t = 2, ct = 3
    WinnerSide tinyint unsigned NOT NULL,
	LoserSide tinyint unsigned NOT NULL,

    RoundEndReason tinyint unsigned NOT NULL,

    StartTick int unsigned NOT NULL,
    EndTick int unsigned NOT NULL,

    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinnerTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LoserTeamID) REFERENCES CS2S_Team(TeamID)
);

CREATE TABLE IF NOT EXISTS CS2S_Death (
    DeathID int unsigned PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    AttackerID bigint unsigned NOT NULL,
    VictimID bigint unsigned NOT NULL,
    AssisterID bigint unsigned, 

    -- t = 2, ct = 3
    AttackerSide tinyint unsigned NOT NULL,
	VictimSide tinyint unsigned NOT NULL,
    AssisterSide tinyint unsigned,

    -- generic = 0, head = 1, chest = 2, stomach = 3, leftarm = 4, rightarm = 5,
    -- leftleg = 6, rightleg = 7, neck = 8, gear = 10
    Hitgroup tinyint unsigned NOT NULL,

    Weapon varchar(32) NOT NULL,
    RoundTick int unsigned NOT NULL,
    OpeningDeath boolean NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (AssisterID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Hurt (
    HurtID int unsigned PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    AttackerID bigint unsigned NOT NULL,
    VictimID bigint unsigned NOT NULL,

    -- t = 2, ct = 3
    AttackerSide tinyint unsigned NOT NULL,
	VictimSide tinyint unsigned NOT NULL,

    -- generic = 0, head = 1, chest = 2, stomach = 3, leftarm = 4, rightarm = 5,
    -- leftleg = 6, rightleg = 7, neck = 8, gear = 10
    Hitgroup tinyint unsigned NOT NULL,

    Damage int unsigned NOT NULL,
    Weapon varchar(32) NOT NULL,
    RoundTick int unsigned NOT NULL,
	FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Blind (
    BlindID int unsigned PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    ThrowerID bigint unsigned NOT NULL,
	BlindedID bigint unsigned NOT NULL,

    -- t = 2, ct = 3
    ThrowerSide tinyint unsigned NOT NULL,
	BlindedSide tinyint unsigned NOT NULL,

    Duration float NOT NULL,
    RoundTick int unsigned NOT NULL,
	FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (ThrowerID) REFERENCES CS2S_PlayerInfo(PlayerID),
	FOREIGN KEY (BlindedID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Grenade (
    GrenadeID int unsigned PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    ThrowerID bigint unsigned NOT NULL,

    -- t = 2, ct = 3
	ThrowerSide int NOT NULL,

    Weapon varchar(32) NOT NULL,
    RoundTick int NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (ThrowerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_KAST (
    KASTID int unsigned PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    PlayerID bigint unsigned NOT NULL,
    
    -- t = 2, ct = 3
	PlayerSide tinyint unsigned NOT NULL,

    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Clutch (
    ClutchID int PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    PlayerID bigint unsigned NOT NULL,

    -- t = 2, ct = 3
	PlayerSide tinyint unsigned NOT NULL,

    EnemyCount tinyint NOT NULL,
    Result ENUM("Win", "Loss") NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Duel (
    DuelID int unsigned PRIMARY KEY AUTO_INCREMENT,
    RoundID int unsigned NOT NULL,
    MatchID int unsigned NOT NULL,

    WinnerID bigint unsigned NOT NULL,
    LoserID bigint unsigned NOT NULL,

    -- t = 2, ct = 3
	WinnerSide smallint unsigned NOT NULL,
    LoserSide smallint unsigned NOT NULL,

    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinnerID) REFERENCES CS2S_PlayerInfo(PlayerID),
	FOREIGN KEY (LoserID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_LivePlayers (
	PlayerID bigint unsigned NOT NULL,
    Kills smallint unsigned DEFAULT 0 NOT NULL,
    Assists smallint unsigned DEFAULT 0 NOT NULL,
    Deaths smallint unsigned DEFAULT 0 NOT NULL,
    ADR float DEFAULT 0 NOT NULL,
	Health tinyint unsigned DEFAULT 0 NOT NULL, 
    Money smallint unsigned DEFAULT 0 NOT NULL,

    -- t = 2, ct = 3
    Side tinyint unsigned NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_LiveStatus (
	StaticID int unsigned PRIMARY KEY,
	MapID varchar(128),

    -- 0 = none (unplanted), 1 = planted, 2 = defused
    BombStatus tinyint unsigned NOT NULL,

    TScore smallint unsigned NOT NULL,
    CTScore smallint unsigned NOT NULL,
	InsertDate datetime DEFAULT CURRENT_TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_Team_Players (
    TeamID varchar(32) NOT NULL,
    PlayerID bigint unsigned NOT NULL,
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Player_Matches (
    PlayerID bigint unsigned NOT NULL,
	MatchID int unsigned NOT NULL,
    PRIMARY KEY (PlayerID, MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID)
);

CREATE TABLE IF NOT EXISTS CS2S_PlayerOfTheWeek (
    PlayerID bigint unsigned NOT NULL,
	WeekPosition int unsigned DEFAULT 0 NOT NULL,
    BaseRating float DEFAULT 0 NOT NULL,
    WeekRating float DEFAULT 0 NOT NULL,
    RatingDelta float DEFAULT 0 NOT NULL,
    PRIMARY KEY (PlayerID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);
