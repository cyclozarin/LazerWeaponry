# LazerWeaponry

*This mod does work with MortalEnemies as a soft depencency, but better to keep it enabled for better experience ^^! And all settings is being synced with host via ConfigSync.*

LazerWeaponry introduces alternative lazer mode to Rescue Hook that is being activated upon hitting R button and also changes item's beam color to red. 

Once you've entered alternative firing mode, you will be able to shoot lazer bullets simillar to what is being fired from Dog's turret and colored to your visor color (that will be visible for all players!)

Alongside with shooting mechanics, mod also introduces standalone PvP mode that can be enabled in settings which also enables headshot system and kill rewarding system that will locally change your amount of cash after submerging back if you killed either monsters or players (monsters give slightly less cash than a players, but that is depends on dangel level of monster and host's settings). 

If PvP mode is being enabled, players will spawn in their separate bells and **only 1 alive player will leave the Old World because noone will be able to submerge unless 1 player is alive.**

Despite Rescue Hook depleting some amount of charge upon firing a projectile, it'll recharge after 3 seconds of no firing if PvP mode is enabled.

## Settings:

### Rescue hook's lazer mode settings
- Bullet damage (integer)
  - Description: Base damage of bullet upon hitting player/enemy.
  - Range: `5..25`
  - Default: `10`
- Ammo (integer)
  - Description: Maximum amount of times that Rescue Hook can be used in battle mode when full-charged.
  - Range: `2..20`
  - Default: `10`
- Recoil force (integer)
  - Description: Recoil force applied to hand upon firing a bullet.
  - Range: `10..50`
  - Default: `25`
- Delay after firing bullet (float)
  - Description: Duration of delay in seconds after firing each bullet.
  - Range: `0.01..0.5`
  - Default: `0.15`
- Damage multiplier on headshot (float)
  - Description: Multiplier of damage that will be applied to victim if bullet will hit their head.
  - Range: `1..4`
  - Default: `2`
- Player fall time upon headshot (float)
  - Description: Duration in seconds of falling down when player is getting a headshot.
  - Range: `0..3`
  - Default: `1.5`
- Money reward for killing player (integer)
  - Description: Amount of money for killing a player.
  - Range: `20..75`
  - Default: `35`
- Money reward for killing monster (integer)
  - Description: Base amount of money for killing a monster.
  - Range: `5..35`
  - Default: `15`

### Sound settings (yes, they exists!)
- Headshot sound volume (integer)
  - Description: Volume of sound that alerts player if they dealt a headshot to another player.
  - Range: `0..100`
  - Default: `75`
- Kill sound volume (integer)
  - Description: Volume of sound that alerts player if they dealt a headshot to another player.
  - Range: `0..100`
  - Default: `100`

### Monster behaviour on hit
- Monster fall time on hit (float)
  - Description: Duration in seconds of falling down when monster is being hit. Note that it doubles upon headshot.
  - Range: `0..5`
  - Default: `2`
- Monster force multiplier on hit (float)
  - Description: Amount of force applied to monsters if they're hit.
  - Range: `0..1`
  - Default: `0.25`

### PvP settings
- PvP mode (toggle)
  - Description: Upon enabling, unlocks some tweaks targeted for multiplayer PvP.
  - Disabled by default
- Vulnerable enemies (toggle)
  - Description: Defines if enemies can be killed via use of MortalEnemies or not. **Won't work when MortalEnemies is disabled in mod manager/isn't installed.**
  - Disabled by default

## Credits

- TheBrickBattla - playtesting and suggesting this mod!
- [Rivinwin](https://github.com/RivinwinCW) - helping with integrating MortalEnemies and playtesting
- [Notest](https://github.com/NotestQ) - helping with integrating ConfigSync and finding annoying bug in Mycelium
- [Day](https://github.com/wwwDayDream) - making awesome WIP mod named TKTC which i had most influence from!