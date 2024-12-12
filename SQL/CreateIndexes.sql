-- CS2S_Map
CREATE INDEX idx_CS2S_Map_MapID ON CS2S_Map (MapID);

-- CS2S_PlayerInfo
CREATE INDEX idx_CS2S_PlayerInfo_PlayerID ON CS2S_PlayerInfo (PlayerID);

-- CS2S_PlayerStats
CREATE INDEX idx_CS2S_PlayerStats_PlayerID ON CS2S_PlayerStats (PlayerID);
CREATE INDEX idx_CS2S_PlayerStats_Side ON CS2S_PlayerStats (Side);

-- CS2S_Match
CREATE INDEX idx_CS2S_Match_MatchID ON CS2S_Match (MatchID);
CREATE INDEX idx_CS2S_Match_MapID ON CS2S_Match (MapID);

-- CS2S_Team
CREATE INDEX idx_CS2S_Team_TeamID ON CS2S_Team (TeamID);

-- CS2S_TeamResult
CREATE INDEX idx_CS2S_TeamResult_TeamID ON CS2S_TeamResult (TeamID);
CREATE INDEX idx_CS2S_TeamResult_MatchID ON CS2S_TeamResult (MatchID);

-- CS2S_Round
CREATE INDEX idx_CS2S_Round_RoundID ON CS2S_Round (RoundID);
CREATE INDEX idx_CS2S_Round_MatchID ON CS2S_Round (MatchID);
CREATE INDEX idx_CS2S_Round_WinnerTeamID ON CS2S_Round (WinnerTeamID);
CREATE INDEX idx_CS2S_Round_LoserTeamID ON CS2S_Round (LoserTeamID);

-- CS2S_Grenade
CREATE INDEX idx_CS2S_Grenade_GrenadeID ON CS2S_Grenade (GrenadeID);
CREATE INDEX idx_CS2S_Grenade_RoundID ON CS2S_Grenade (RoundID);
CREATE INDEX idx_CS2S_Grenade_MatchID ON CS2S_Grenade (MatchID);
CREATE INDEX idx_CS2S_Grenade_ThrowerID ON CS2S_Grenade (ThrowerID);

-- CS2S_Clutch
CREATE INDEX idx_CS2S_Clutch_ClutchID ON CS2S_Clutch (ClutchID);
CREATE INDEX idx_CS2S_Clutch_RoundID ON CS2S_Clutch (RoundID);
CREATE INDEX idx_CS2S_Clutch_MatchID ON CS2S_Clutch (MatchID);
CREATE INDEX idx_CS2S_Clutch_PlayerID ON CS2S_Clutch (PlayerID);

-- CS2S_Duel
CREATE INDEX idx_CS2S_Duel_DuelID ON CS2S_Duel (DuelID);
CREATE INDEX idx_CS2S_Duel_RoundID ON CS2S_Duel (RoundID);
CREATE INDEX idx_CS2S_Duel_MatchID ON CS2S_Duel (MatchID);
CREATE INDEX idx_CS2S_Duel_WinnerID ON CS2S_Duel (WinnerID);
CREATE INDEX idx_CS2S_Duel_LoserID ON CS2S_Duel (LoserID);

-- CS2S_Live
CREATE INDEX idx_CS2S_Live_StaticID ON CS2S_Live (StaticID);

-- CS2S_Team_Players
CREATE INDEX idx_CS2S_Team_Players_TeamID ON CS2S_Team_Players (TeamID);
CREATE INDEX idx_CS2S_Team_Players_PlayerID ON CS2S_Team_Players (PlayerID);

-- CS2S_Player_Matches
CREATE INDEX idx_CS2S_Player_Matches_PlayerID ON CS2S_Player_Matches (PlayerID);
CREATE INDEX idx_CS2S_Player_Matches_MatchID ON CS2S_Player_Matches (MatchID);

-- CS2S_Hurt
CREATE INDEX idx_CS2S_Hurt_HurtID ON CS2S_Hurt (HurtID);
CREATE INDEX idx_CS2S_Hurt_RoundID ON CS2S_Hurt (RoundID);
CREATE INDEX idx_CS2S_Hurt_MatchID ON CS2S_Hurt (MatchID);
CREATE INDEX idx_CS2S_Hurt_VictimID ON CS2S_Hurt (VictimID);
CREATE INDEX idx_CS2S_Hurt_AttackerID ON CS2S_Hurt (AttackerID);

-- CS2S_Death
CREATE INDEX idx_CS2S_Death_DeathID ON CS2S_Death (DeathID);
CREATE INDEX idx_CS2S_Death_MatchID ON CS2S_Death (MatchID);
CREATE INDEX idx_CS2S_Death_AttackerID ON CS2S_Death (AttackerID);
CREATE INDEX idx_CS2S_Death_AssisterID ON CS2S_Death (AssisterID);
CREATE INDEX idx_CS2S_Death_VictimID ON CS2S_Death (VictimID);
CREATE INDEX idx_CS2S_Death_RoundID ON CS2S_Death (RoundID);

-- CS2S_Blind
CREATE INDEX idx_CS2S_Blind_BlindID ON CS2S_Blind (BlindID);
CREATE INDEX idx_CS2S_Blind_RoundID ON CS2S_Blind (RoundID);
CREATE INDEX idx_CS2S_Blind_MatchID ON CS2S_Blind (MatchID);
CREATE INDEX idx_CS2S_Blind_BlindedID ON CS2S_Blind (BlindedID);
CREATE INDEX idx_CS2S_Blind_ThrowerID ON CS2S_Blind (ThrowerID);

-- CS2S_KAST
CREATE INDEX idx_CS2S_KAST_KASTID ON CS2S_KAST (KASTID);
CREATE INDEX idx_CS2S_KAST_RoundID ON CS2S_KAST (RoundID);
CREATE INDEX idx_CS2S_KAST_MatchID ON CS2S_KAST (MatchID);
CREATE INDEX idx_CS2S_KAST_PlayerID ON CS2S_KAST (PlayerID);
