-- Create CS2S_Map table first as it is referenced by CS2S_Match
CREATE TABLE IF NOT EXISTS CS2S_Map (
    MapID int PRIMARY KEY NOT NULL,
    Name varchar(128)
);

-- Create CS2S_Team next as it is referenced by CS2S_Match and CS2S_Round
CREATE TABLE IF NOT EXISTS CS2S_Team (
    TeamID int PRIMARY KEY AUTO_INCREMENT,
    Name varchar(64) DEFAULT "Team"
);

-- Create CS2S_Player since it is referenced by other tables like CS2S_Death and CS2S_Hurt
CREATE TABLE IF NOT EXISTS CS2S_Player (
    PlayerID varchar(17) PRIMARY KEY NOT NULL,
    Username varchar(255) NOT NULL,
    AvatarS varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e.jpg",
    AvatarM varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_medium.jpg",
    AvatarL varchar(255) DEFAULT "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_full.jpg",
    ELO int DEFAULT 1000,
    Kills int DEFAULT 0,
    Headshots int DEFAULT 0,
    Assists int DEFAULT 0,
    Deaths int DEFAULT 0,
    TotalDamage int DEFAULT 0,
    UtilityDamage int DEFAULT 0,
    RoundsKAST int DEFAULT 0,
    RoundsPlayed int DEFAULT 0
);

-- Create CS2S_Match table which references CS2S_Map and CS2S_Team
CREATE TABLE IF NOT EXISTS CS2S_Match (
    MatchID int PRIMARY KEY AUTO_INCREMENT,
    MapID int,
    WinningTeamID int,
    LosingTeamID int,
    WinningTeamScore int,
    LosingTeamScore int,
    DeltaELO int DEFAULT 1000,
    FOREIGN KEY (MapID) REFERENCES CS2S_Map(MapID),
    FOREIGN KEY (WinningTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LosingTeamID) REFERENCES CS2S_Team(TeamID)
);

-- Create CS2S_Round table which references CS2S_Match and CS2S_Team
CREATE TABLE IF NOT EXISTS CS2S_Round (
    RoundID int PRIMARY KEY AUTO_INCREMENT,
    MatchID int,
    WinningTeamID int,
    LosingTeamID int,
    RoundEndReason int,
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (WinningTeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (LosingTeamID) REFERENCES CS2S_Team(TeamID)
);

-- Create CS2S_Death table which references CS2S_Player and CS2S_Round
CREATE TABLE IF NOT EXISTS CS2S_Death (
    DeathID int PRIMARY KEY AUTO_INCREMENT,
    RoundID int,
    AttackerID varchar(17),
    AssisterID varchar(17),
    VictimID varchar(17),
    Weapon varchar(32),
    Hitgroup int,
    Assistedflash BOOLEAN,
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID),
    FOREIGN KEY (AttackerID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (AssisterID) REFERENCES CS2S_Player(PlayerID),
    FOREIGN KEY (VictimID) REFERENCES CS2S_Player(PlayerID)
);

-- Create CS2S_Hurt table which references CS2S_Player
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

-- Create CS2S_Live table with MyISAM engine for live match tracking
CREATE TABLE IF NOT EXISTS CS2S_Live (
    TPlayers text, -- This can be a JSON object serialized as a string
    CTPlayers text, -- This can be a JSON object serialized as a string
    TScore int,
    CTScore int,
    BombStatus int,
    RoundTime int
) ENGINE=MyISAM;

-- Create CS2S_Match_Rounds table which references CS2S_Match and CS2S_Round
CREATE TABLE IF NOT EXISTS CS2S_Match_Rounds (
    MatchID int,
    RoundID int,
    PRIMARY KEY (MatchID, RoundID),
    FOREIGN KEY (MatchID) REFERENCES CS2S_Match(MatchID),
    FOREIGN KEY (RoundID) REFERENCES CS2S_Round(RoundID)
);

-- Create CS2S_Team_Players table which references CS2S_Team and CS2S_Player
CREATE TABLE IF NOT EXISTS CS2S_Team_Players (
    TeamID int,
    PlayerID varchar(17),
    PRIMARY KEY (TeamID, PlayerID),
    FOREIGN KEY (TeamID) REFERENCES CS2S_Team(TeamID),
    FOREIGN KEY (PlayerID) REFERENCES CS2S_Player(PlayerID)
);
