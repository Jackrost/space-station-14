
## Cult

# Shown at the end of a round of Cult
cult-round-end-result = {$cultistCount ->
    [one] There was one cultist.
    *[other] There were {$cultistCount} cultists.
}
# Shown at the end of a round of Cult
cult-user-was-a-cultist = [color=gray]{$user}[/color] was a cultist.
cult-user-was-a-cultist-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a cultist.
cult-was-a-cultist-named = [color=White]{$name}[/color] was a cultist.

-------- Change this!!!!!!!!!! ------------
cult-user-was-a-traitor-with-objectives = [color=gray]{$user}[/color] was a traitor who had the following objectives:
cult-user-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a traitor who had the following objectives:
cult-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] was a traitor who had the following objectives:


preset-cult-objective-issuer-cult = [color=#87cefa]The Cult[/color]
# Shown at the end of a round of Cult
cult-objective-condition-success = {$condition} | [color={$markupColor}]Success![/color]
# Shown at the end of a round of Cult
cult-objective-condition-fail = {$condition} | [color={$markupColor}]Failure![/color] ({$progress}%)
cult-title = Cult
cult-description = There are cultists among us...
cult-not-enough-ready-players = Not enough players readied up for the game! There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed.
cult-no-one-ready = No players readied up! Can't start Cult.

## CultRole

# CultRole
cult-role-greeting =
    You are a cultist.
    Your objectives and list of dark words are listed in the character menu.
    Use dark words from book to draw runes with your blood. Cooperate with other cultists and sacrifice victims to find more words.
    Convert victims to followers to help the Cult grew bigger.
    Hail to the order of dark god Nar-Sie!
cult-role-cultwords =
    The dark words are:
    {$cultwords}
    Use dark words to draw runes. Experiment with unknown words to find out correct translation.
