DELIMITER $$
CREATE TRIGGER after_round_insert
AFTER INSERT ON CS2S_Round
FOR EACH ROW
BEGIN
    -- Increment RoundsPlayed for all players in the winning team
    UPDATE CS2S_Player
    SET RoundsPlayed = RoundsPlayed + 1
    WHERE PlayerID IN (
        SELECT PlayerID FROM CS2S_Team_Players WHERE TeamID = NEW.WinningTeamID
    );

    -- Increment RoundsPlayed for all players in the losing team
    UPDATE CS2S_Player
    SET RoundsPlayed = RoundsPlayed + 1
    WHERE PlayerID IN (
        SELECT PlayerID FROM CS2S_Team_Players WHERE TeamID = NEW.LosingTeamID
    );
END$$

CREATE TRIGGER after_match_insert
AFTER INSERT ON CS2S_Match
FOR EACH ROW
BEGIN
    -- Increment MatchesPlayed for all players in the winning team
    UPDATE CS2S_Player
    SET MatchesPlayed = MatchesPlayed + 1
    WHERE PlayerID IN (
        SELECT PlayerID FROM CS2S_Team_Players WHERE TeamID = NEW.WinningTeamID
    );

    -- Increment MatchesPlayed for all players in the losing team
    UPDATE CS2S_Player
    SET MatchesPlayed = MatchesPlayed + 1
    WHERE PlayerID IN (
        SELECT PlayerID FROM CS2S_Team_Players WHERE TeamID = NEW.LosingTeamID
    );
END$$

CREATE TRIGGER after_death_insert
AFTER INSERT ON CS2S_Death
FOR EACH ROW
BEGIN
    -- Increment Kills for the attacker
    IF NEW.AttackerID IS NOT NULL THEN
        UPDATE CS2S_Player
        SET Kills = Kills + 1
        WHERE PlayerID = NEW.AttackerID;
    END IF;

    -- Increment Assists for the assister
    IF NEW.AssisterID IS NOT NULL THEN
        UPDATE CS2S_Player
        SET Assists = Assists + 1
        WHERE PlayerID = NEW.AssisterID;
    END IF;

    -- Increment Deaths for the victim
    IF NEW.VictimID IS NOT NULL THEN
        UPDATE CS2S_Player
        SET Deaths = Deaths + 1
        WHERE PlayerID = NEW.VictimID;
    END IF;
END$$

DELIMITER $$

CREATE TRIGGER after_hurt_insert
AFTER INSERT ON CS2S_Hurt
FOR EACH ROW
BEGIN
    -- Check the weapon type and update the corresponding damage column
    IF NEW.Weapon IN ('smokegrenade', 'hegrenade', 'flashbang', 'molotov', 'inferno', 'decoy') THEN
        -- Add to UtilityDamage
        UPDATE CS2S_Player
        SET UtilityDamage = UtilityDamage + NEW.DamageAmount
        WHERE PlayerID = NEW.AttackerID;
    ELSE
        -- Add to TotalDamage
        UPDATE CS2S_Player
        SET TotalDamage = TotalDamage + NEW.DamageAmount
        WHERE PlayerID = NEW.AttackerID;
    END IF;
END$$

DELIMITER ;
