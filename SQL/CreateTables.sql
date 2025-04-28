CREATE TABLE IF NOT EXISTS CS2S_Map (
    MapID varchar(128) PRIMARY KEY NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_PlayerInfo (
    PlayerID bigint PRIMARY KEY NOT NULL,
	ELO int DEFAULT 1000 NOT NULL,
    Username varchar(255) NOT NULL,
    AvatarS varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e.jpg" NOT NULL,
    AvatarM varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_medium.jpg" NOT NULL,
    AvatarL varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_full.jpg" NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_Match (
    MatchID int PRIMARY KEY NOT NULL,
    MapID varchar(128) NOT NULL,
    StartTick int NOT NULL,
    EndTick int NOT NULL,
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
	Score int NOT NULL,
    Result ENUM("Win", "Loss", "Tie") NOT NULL,
    Side int NOT NULL,
    DeltaELO int DEFAULT 0 NOT NULL,
    PRIMARY KEY (TeamID, MatchID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID)
);

CREATE TABLE IF NOT EXISTS CS2S_Round (
    RoundID int PRIMARY KEY NOT NULL,
    MatchID int NOT NULL,
    WinnerTeamID varchar(32) NOT NULL,
    LoserTeamID varchar(32) NOT NULL,
    WinnerSide int NOT NULL,
	LoserSide int NOT NULL,
    RoundEndReason int NOT NULL,
    StartTick int NOT NULL,
    EndTick int NOT NULL,
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinnerTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LoserTeamID) REFERENCES CS2S_Team(TeamID)
);

CREATE TABLE IF NOT EXISTS CS2S_Death (
    DeathID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    AttackerID bigint NULL,
    AttackerSide int NULL,
    AssisterID bigint NULL,
    AssisterSide int NULL,
    VictimID bigint NOT NULL,
	VictimSide int NOT NULL,
    Weapon varchar(32) NOT NULL,
    Hitgroup int NOT NULL,
    RoundTick int NOT NULL,
    OpeningDeath bool NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (AssisterID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Hurt (
    HurtID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    AttackerID bigint NULL,
    AttackerSide int NULL,
    VictimID bigint NOT NULL,
	VictimSide int NULL,
    Damage int NOT NULL,
    Weapon varchar(32) NOT NULL,
    Hitgroup int NOT NULL,
    RoundTick int NOT NULL,
	FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Blind (
    BlindID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    ThrowerID bigint NOT NULL,
	ThrowerSide int NOT NULL,
	BlindedID bigint NOT NULL,
	BlindedSide int NOT NULL,
    Duration float NOT NULL,
    RoundTick int NOT NULL,
	FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (ThrowerID) REFERENCES CS2S_PlayerInfo(PlayerID),
	FOREIGN KEY (BlindedID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Grenade (
    GrenadeID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    ThrowerID bigint NOT NULL,
	ThrowerSide int NOT NULL,
    Weapon varchar(32) NOT NULL,
    RoundTick int NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (ThrowerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_KAST (
    KASTID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    PlayerID bigint NOT NULL,
	PlayerSide int NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Clutch (
    ClutchID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    PlayerID bigint NOT NULL,
	PlayerSide int NOT NULL,
    EnemyCount int NOT NULL,
    Result ENUM("Win", "Loss") NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
	FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Duel (
    DuelID int PRIMARY KEY AUTO_INCREMENT NOT NULL,
    RoundID int NOT NULL,
    MatchID int NOT NULL,
    WinnerID bigint NOT NULL,
	WinnerSide int NOT NULL,
    LoserID bigint NOT NULL,
    LoserSide int NOT NULL,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinnerID) REFERENCES CS2S_PlayerInfo(PlayerID),
	FOREIGN KEY (LoserID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_LivePlayers (
	PlayerID bigint NOT NULL,
    Kills int DEFAULT 0 NOT NULL,
    Assists int DEFAULT 0 NOT NULL,
    Deaths int DEFAULT 0 NOT NULL,
    ADR float DEFAULT 0 NOT NULL,
	Health int DEFAULT 0 NOT NULL, 
    Money int DEFAULT 0 NOT NULL,
    Side int NOT NULL
);

CREATE TABLE IF NOT EXISTS CS2S_LiveStatus (
	StaticID int PRIMARY KEY,
	MapID varchar(128),
    BombStatus int,
    TScore int,
    CTScore int,
	InsertDate datetime DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS CS2S_Team_Players (
    TeamID varchar(32) NOT NULL,
    PlayerID bigint NOT NULL,
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);

CREATE TABLE IF NOT EXISTS CS2S_Player_Matches (
    PlayerID bigint NOT NULL,
	MatchID int NOT NULL,
    PRIMARY KEY (PlayerID, MatchID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID)
);

CREATE TABLE IF NOT EXISTS CS2S_PlayerOfTheWeek (
    PlayerID bigint NOT NULL,
	WeekPosition int DEFAULT 0 NOT NULL,
    BaseRating float DEFAULT 0 NOT NULL,
    WeekRating float DEFAULT 0 NOT NULL,
    RatingDelta float DEFAULT 0 NOT NULL,
    PRIMARY KEY (PlayerID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_PlayerInfo(PlayerID)
);


