using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using PanoramaVote; // Add the PanoramaVote namespace

namespace SLAYER_PanoramaVote;

#pragma warning disable CS8618
public partial class SLAYER_PanoramaVote : BasePlugin //, IPluginConfig<SLAYER_VotesConfig>
{
    public override string ModuleName => "SLAYER_PanoramaVote";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "SLAYER";
    public override string ModuleDescription => "Panorama Votes";
    public CPanoramaVote voteHandler; // Global variable to hold the vote handler
    string Prefix = $" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Green}SLAYER_PanoramaVote {ChatColors.DarkRed}★{ChatColors.Gold}]";
    public override void Load(bool hotReload)
    {
        voteHandler = new CPanoramaVote(this); // Initialize the vote handler
        RegisterEventHandler<EventVoteCast>((@event, info) =>
		{
            voteHandler.VoteCast(@event); // Call the vote cast function that vote has been casted by player

			return HookResult.Continue;
		});
    }

    [ConsoleCommand("css_test")]
    public void OnCommandTest(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null)
            return;

        voteHandler.Init(); // Initialize the vote handler
        // Send a vote to a specific player(s)
        //RecipientFilter filter = new RecipientFilter{player};
        //voteHandler.SendYesNoVote(30.0f, VoteConstants.VOTE_CALLER_SERVER, "#SFUI_vote_panorama_vote_default", $"Hold on, Let me Cook", filter, VoteResultCallback, VoteHandlerCallback);
        // Send a vote to all players
        //voteHandler.SendYesNoVoteToAll(30.0f, VoteConstants.VOTE_CALLER_SERVER, "#SFUI_vote_changelevel", "de_dust2", VoteResultCallback, VoteHandlerCallback);
        voteHandler.SendYesNoVoteToAll(30.0f, VoteConstants.VOTE_CALLER_SERVER, "#SFUI_vote_panorama_vote_red", $"Hold on, Let me Cook", VoteResultCallback, VoteHandlerCallback);
    }
    private bool VoteResultCallback(YesNoVoteInfo info)
    {
        Server.PrintToChatAll($" {Prefix} {ChatColors.Green}VoteResultCallback: {ChatColors.Red}NO votes = {ChatColors.Green}{info.no_votes} {ChatColors.White}| {ChatColors.Red}YES votes = {ChatColors.Green}{info.yes_votes} {ChatColors.White}| {ChatColors.Red}total votes = {ChatColors.Green}{info.num_votes} {ChatColors.White}| {ChatColors.Red}Total Voters = {ChatColors.Green}{info.num_clients}");

        foreach (var kvp in info.clientInfo) // Print the vote info for each player
        {
            Console.WriteLine($"Player in Key: {kvp.Key}: Player Slot = {kvp.Value.Item1}, Player Vote = {(kvp.Value.Item2 == (int)CastVote.VOTE_OPTION1 ? "Yes" : "No")}");
        }
        if(info.yes_votes > info.no_votes) // Check if the vote passed
        {
            Server.PrintToChatAll($" {Prefix} {ChatColors.Green}VoteResultCallback: Vote passed!");
            //Server.PrintToChatAll($" {Prefix} {ChatColors.Green}VoteResultCallback: Vote passed! Changing map to de_dust2");
            // Vote Passed now change map to de_dust2
            //Server.ExecuteCommand("css_map de_dust2");
            return true;
        }
        else
        {
            Server.PrintToChatAll($" {Prefix} {ChatColors.Red}VoteResultCallback: Vote failed!");
            return false;
        }
    }
    private void VoteHandlerCallback(YesNoVoteAction action, int param1, int param2)
    {
        switch (action)
        {
            case YesNoVoteAction.VoteAction_Start: // On Vote Start
            {
                Server.PrintToChatAll($" {Prefix} {ChatColors.Green}VoteHandlerCallback: Vote started!");
                break;
            }
            case YesNoVoteAction.VoteAction_Vote: // On Player Vote: param1 = client slot, param2 = choice (VOTE_OPTION1=yes, VOTE_OPTION2=no)
            {
                CCSPlayerController player = Utilities.GetPlayerFromSlot(param1)!;
                if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
                    break;
                player.PrintToChat($" {Prefix} {ChatColors.Lime}{player.PlayerName} {ChatColors.White}Thanks for voting! You voted: {(param2 == (int)CastVote.VOTE_OPTION1 ? $"{ChatColors.Green}Yes" : $"{ChatColors.Red}No")}");
                break;
            }
            case YesNoVoteAction.VoteAction_End:
            {
                if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_Cancelled) // Vote Cancelled
                {
                    Server.PrintToChatAll($" {Prefix} {ChatColors.Red}VoteHandlerCallback: Vote Ended! Cancelled");
                }
                else if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_AllVotes) // Everyone Voted
                {
                    Server.PrintToChatAll($" {Prefix} {ChatColors.Green}VoteHandlerCallback: Vote Ended! Thank you for participating.");
                }
                else if ((YesNoVoteEndReason)param1 == YesNoVoteEndReason.VoteEnd_TimeUp) // Time is up
                {
                    Server.PrintToChatAll($" {Prefix} {ChatColors.Red}VoteHandlerCallback: Vote Ended! Time is up.");
                }

                break;
            }
        }
    }
}
    