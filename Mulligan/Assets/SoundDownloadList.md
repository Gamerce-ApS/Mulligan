# Mulligan Sound Download List

## Goal

Download a focused sound set for Mulligan before building `SoundManager`.
The style should be a polished fantasy card battler: tactile cards, soft magic, clean mobile UI, and short satisfying feedback sounds.

## Must-Have SFX

### UI

- [ ] UI tap/click: soft wooden/card-game button tap, 3-5 variations.
- [ ] Window open: light whoosh/pop for shop, inventory, deck overview, hero select.
- [ ] Window close: reverse whoosh/down-swipe.
- [ ] Tooltip/info popup: small paper flick or soft chime.
- [ ] Error/blocked action: dull tap or muted thunk for no gold, no slots, invalid tutorial click.
- [ ] Success/confirm: bright short chime for purchases, hero play, reward accepted.

### Cards

- [ ] Card select: crisp card lift/tap.
- [ ] Card deselect: softer card drop.
- [ ] Card draw from deck: paper/card slide, 4-6 variations.
- [ ] Card move/fly: quick card whoosh.
- [ ] Card discard: card into pile, papery slap.
- [ ] Hand reroll: shuffle burst, slightly longer than draw.
- [ ] Deck shuffle: fuller shuffle sound.

### Combat

- [ ] Attack button / play hand: committed action sound, low thump plus card flare.
- [ ] Damage number added to total: small tick/count-up hit.
- [ ] Crit added: sharper golden sparkle or metallic ping.
- [ ] Enemy takes damage: impact hit, 3-5 variations.
- [ ] Hero takes damage: heavier body hit, 2-3 variations.
- [ ] Enemy attack wind-up: short whoosh/lunge.
- [ ] Enemy death: collapse/poof.
- [ ] Dodge/evasion: quick airy swipe.

### Potions

- [ ] Potion pick/tap: glass clink.
- [ ] Potion drag/use: liquid swirl or magical bottle pop.
- [ ] Potion heal: soft healing shimmer.
- [ ] Potion damage/crit buff: magical charge.
- [ ] Potion destroy/explode effect: small burst.

### Artifacts, Runes, Upgrades

- [ ] Artifact obtained: relic pickup chime.
- [ ] Artifact trigger: magical/metallic pulse.
- [ ] Artifact sold: coin clink.
- [ ] Rune obtained/equipped: deeper mystical rune tone.
- [ ] Rune trigger: short rune hum.
- [ ] Unit upgrade selected: enchantment sparkle.
- [ ] Unit upgrade applied: satisfying magic stamp/upgrade burst.
- [ ] Rank up: rising chime.

### Shop And Rewards

- [ ] Gold gained: coin tick or coin spill.
- [ ] Shop reroll: market shuffle/refresh.
- [ ] Shop purchase: coin spend plus confirm.
- [ ] Shop item drag start: pickup tick.
- [ ] Shop item drop/cancel: soft return thunk.
- [ ] Battle from shop: battle-ready button hit.
- [ ] Victory screen: short win flourish, 1-2 seconds.
- [ ] Lose screen: short defeat sting, not too harsh.
- [ ] Boss intro: ominous riser/stinger.
- [ ] Level selection open: map/page reveal.
- [ ] Unlock reveal window: special magical reveal swell.
- [ ] Each unlocked item reveal: sparkle/pop, can reuse success with variation.
- [ ] Inventory/deck overview open: book/page open.

## Nice-To-Have Music And Ambience

- [ ] Main menu/splash loop: calm fantasy/card-table loop.
- [ ] Gameplay loop: subtle low-intensity loop, no strong melody.
- [ ] Shop loop: cozy market/tavern loop.
- [ ] Boss round loop or layer: darker percussion/drone.
- [ ] Victory ambience sting.
- [ ] Defeat ambience sting.

## Download Guidelines

- Use WAV for short SFX.
- Use OGG for music and ambience.
- Keep repeated UI/card sounds dry and short, usually under 0.5 seconds.
- Prefer multiple variations for card draw, card tap, damage, and coin sounds.
- Avoid long reverb tails on frequent sounds; mobile audio gets muddy fast.
- Good search tags: `card game`, `paper`, `fantasy UI`, `magic spell UI`, `coin`, `RPG combat`, `potion`, `rune`, `victory sting`.

## First Batch To Download

1. Fantasy UI click pack.
2. Card/paper handling pack.
3. Magic spell UI pack.
4. Coin/shop pack.
5. RPG impact/combat pack.
6. Short victory/defeat/boss stinger pack.
7. One subtle fantasy music loop pack.

## Later SoundManager Notes

- Support random clip variations.
- Support pitch variation for repeated sounds.
- Support separate volumes for UI, cards, combat, music, and ambience.
- Keep one-shot SFX simple to call from existing manager methods.
