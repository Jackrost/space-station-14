using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Cult;
using Content.Server.Cult.Components;
using Content.Server.MobState;
using Content.Server.Actions;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Microsoft.CodeAnalysis;

namespace Content.Server.GameTicking.Rules;

public sealed class CultRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    public override string Prototype => "Cult";

    private readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/bloodcult.ogg");
    public List<CultRole> Cultists = new();

    private const string CultistPrototypeID = "Cult";

    public int TotalCultists => Cultists.Count;
    //public string[] CultWords = new string[3];

    private int _playersPerCultist => _cfg.GetCVar(CCVars.CultPlayersPerCultists);
    private int _maxCultists => _cfg.GetCVar(CCVars.CultMaxCultists);

    Dictionary<string,int> CultWordsKey = new Dictionary<string, int>() { { "1", 1 } , {"2", 1} };
    List<string> CultWordsValue = new List<string>() { "a", "b", "c" };

    public Dictionary<string, string> CultWordsDictionary = new Dictionary<string, string>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        //SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    public override void Started() { }

    public override void Ended()
    {
        Cultists.Clear();
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        //MakeCultWords();
        if (!RuleAdded)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.CultMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("cult-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("cult-no-one-ready"));
            ev.Cancel();
            return;
        }
    }

    /*
    private void MakeCultWords()
    {
        var cultwordkeyPool = CultWordsKey;
        var cultwordvaluePool = CultWordsValue;
        var listCount = Math.Min(cultwordkeyPool.Count, cultwordkeyPool.Count);
        for (var i = 0; i < listCount; i++)
        {
            var skey = _random.PickAndTake(cultwordkeyPool);
            var svalue = _random.PickAndTake(cultwordvaluePool);
            CultWordsDictionary.TryAdd<string,string>(skey,svalue);
        }
    }
    */

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        var numCultists = MathHelper.Clamp(ev.Players.Length / _playersPerCultist, 1, _maxCultists);
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);

        var cultistPool = FindPotentialCultist(ev);
        var selectedCultists = PickCultists(numCultists, cultistPool);

        foreach (var cultist in selectedCultists)
            MakeCultist(cultist);
    }

    public List<IPlayerSession> FindPotentialCultist(RulePlayerJobsAssignedEvent ev)
    {
        var list = new List<IPlayerSession>(ev.Players).Where(x =>
            x.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false
        ).ToList();

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(CultistPrototypeID))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient preferred cultists, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    public List<IPlayerSession> PickCultists(int cultistCount, List<IPlayerSession> prefList)
    {
        var results = new List<IPlayerSession>(cultistCount);
        if (prefList.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient ready players to fill up with cultists, stopping the selection.");
            return results;
        }

        for (var i = 0; i < cultistCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            Logger.InfoS("preset", "Selected a preferred cultist.");
        }
        return results;
    }

    public bool MakeCultist(IPlayerSession cultist)
    {
        var mind = cultist.Data.ContentData()?.Mind;
        if (mind == null)
        {
            Logger.ErrorS("preset", "Failed getting mind for picked cultist.");
            return false;
        }

        /*
        // creadth: we need to create uplink for the antag.
        // PDA should be in place already
        DebugTools.AssertNotNull(mind.OwnedEntity);

        var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);

        if (mind.CurrentJob != null)
            startingBalance = Math.Max(startingBalance - mind.CurrentJob.Prototype.AntagAdvantage, 0);

        if (!_uplink.AddUplink(mind.OwnedEntity!.Value, startingBalance))
            return false;
        */

        // Give ritual knife and metal to satchel or pocket                            -----------------------------------------------------------------------------------

        // Give Commune ability
        if (mind.OwnedEntity != null)
        {
            var EntityUid = mind.OwnedEntity;
            _action.AddAction(EntityUid, "CultCommune", null);
        }
        

        // Pick words from list of cult words
        //Dictionary<string, string> cultWordsList = new Dictionary<string, string>();


        // Add component

        //EntityManager.AddComponent<CultComponent>(mind.OwnedEntity);

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(CultistPrototypeID);
        var cultistRole = new CultRole(mind, antagPrototype);
        mind.AddRole(cultistRole);
        Cultists.Add(cultistRole);
        cultistRole.GreetCultist();

        var maxPicks = _cfg.GetCVar(CCVars.CultMaxPicks);

        //Give cultist their objectives
        for (var pick = 0; pick < maxPicks; pick++)
        {
            var objective = _objectivesManager.GetRandomObjective(cultistRole.Mind, "TraitorObjectiveGroups");      //  ----------------------------------------------------------------------------------- Change this
            if (objective == null) continue;
            cultistRole.Mind.TryAddObjective(objective);
        }

        
        //give traitors their codewords to keep in their character info menu
        //cultistRole.Mind.Briefing = Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", Codewords)));

        SoundSystem.Play(_addedSound.GetSound(), Filter.Empty().AddPlayer(cultist), AudioParams.Default);
        return true;
    }

    public bool RemoveCultist(IPlayerSession cultist)
    {
        var mind = cultist.Data.ContentData()?.Mind;
        if (mind == null)
        {
            Logger.ErrorS("preset", "Failed getting mind for picked cultist.");
            return false;
        }

        // TO-DO
        // ---- Remove red eyes
        // ---- Remove bloody red halo

        //mind.RemoveRole();
        //Cultists.Remove();

        // Message person that he is no longer a cultist


        return true;
    }

        private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;
        if (TotalCultists >= _maxCultists)
            return;
        if (!ev.LateJoin)
            return;
        if (!ev.Profile.AntagPreferences.Contains(CultistPrototypeID))
            return;


        if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        if (!job.CanBeAntag)
            return;

        // the nth player we adjust our probabilities around
        int target = ((_playersPerCultist * TotalCultists) + 1);

        float chance = (1f / _playersPerCultist);

        /// If we have too many cultist, divide by how many players below target for next cultist we are.
        if (ev.JoinOrder < target)
        {
            chance /= (target - ev.JoinOrder);
        }
        else // Tick up towards 100% chance.
        {
            chance *= ((ev.JoinOrder + 1) - target);
        }
        if (chance > 1)
            chance = 1;

        // Now that we've calculated our chance, roll and make them a cultist if we roll under.
        // You get one shot.
        if (_random.Prob((float) chance))
        {
            MakeCultist(ev.Player);
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var result = Loc.GetString("cult-round-end-result", ("cultistCount", Cultists.Count));

        foreach (var cultist in Cultists)
        {
            var name = cultist.Mind.CharacterName;
            cultist.Mind.TryGetSession(out var session);
            var username = session?.Name;

            var objectives = cultist.Mind.AllObjectives.ToArray();
            if (objectives.Length == 0)
            {
                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("cult-user-was-a-cultist", ("user", username));
                    else
                        result += "\n" + Loc.GetString("cult-user-was-a-cultist-named", ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("cult-was-a-cultist-named", ("name", name));

                continue;
            }

            if (username != null)
            {
                if (name == null)
                    result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives", ("user", username));
                else
                    result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives-named", ("user", username), ("name", name));
            }
            else if (name != null)
                result += "\n" + Loc.GetString("traitor-was-a-traitor-with-objectives-named", ("name", name));

            foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
            {
                result += "\n" + Loc.GetString($"preset-cult-objective-issuer-{objectiveGroup.Key}");

                foreach (var objective in objectiveGroup)
                {
                    foreach (var condition in objective.Conditions)
                    {
                        var progress = condition.Progress;
                        if (progress > 0.99f)
                        {
                            result += "\n- " + Loc.GetString(
                                "traitor-objective-condition-success",
                                ("condition", condition.Title),
                                ("markupColor", "green")
                            );
                        }
                        else
                        {
                            result += "\n- " + Loc.GetString(
                                "traitor-objective-condition-fail",
                                ("condition", condition.Title),
                                ("progress", (int) (progress * 100)),
                                ("markupColor", "red")
                            );
                        }
                    }
                }
            }
        }
        ev.AddLine(result);
    }

    public IEnumerable<Cult.CultRole> GetOtherTraitorsAliveAndConnected(Mind.Mind ourMind)
    {
        var cultists = Cultists;
        List<Cult.CultRole> removeList = new();

        return Cultists // don't want
            .Where(t => t.Mind is not null) // no mind
            .Where(t => t.Mind.OwnedEntity is not null) // no entity
            .Where(t => t.Mind.Session is not null) // player disconnected
            .Where(t => t.Mind != ourMind) // ourselves
            .Where(t => _mobStateSystem.IsAlive((EntityUid) t.Mind.OwnedEntity!)) // dead
            .Where(t => t.Mind.CurrentEntity == t.Mind.OwnedEntity); // not in original body
    }
}
