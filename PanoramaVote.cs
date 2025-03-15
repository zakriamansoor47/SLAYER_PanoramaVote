using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;


namespace PanoramaVote;

public enum CastVote
{
    VOTE_NOTINCLUDED = -1,
    VOTE_OPTION1,  // Use this for Yes
    VOTE_OPTION2,  // Use this for No
    VOTE_OPTION3,
    VOTE_OPTION4,
    VOTE_OPTION5,
    VOTE_UNCAST = 5
}

public enum YesNoVoteEndReason
{
    VoteEnd_AllVotes,  // All possible votes were cast
    VoteEnd_TimeUp,    // Time ran out
    VoteEnd_Cancelled  // The vote got cancelled
}

public enum YesNoVoteAction
{
    VoteAction_Start,  // nothing passed
    VoteAction_Vote,   // param1 = client slot, param2 = choice (VOTE_OPTION1 = yes, VOTE_OPTION2 = no)
    VoteAction_End     // param1 = YesNoVoteEndReason reason why the vote ended
}

public class YesNoVoteInfo
{
    public int num_votes;                // Number of votes tallied in total
    public int yes_votes;                // Number of votes for yes
    public int no_votes;                 // Number of votes for no
    public int num_clients;              // Number of clients who could vote
    //public int[,] clientInfo = new int[MAXPLAYERS, 2];  // Client voting info, user VOTEINFO_CLIENT_ defines. Anything >= [num_clients] is VOTE_NOTINCLUDED, VOTE_UNCAST = client didn't vote
    public Dictionary<int, (int, int)> clientInfo = new Dictionary<int, (int, int)>();
}
public class VoteConstants
{
    public const int VOTE_CALLER_SERVER = 99;
    public const int VOTE_NOTINCLUDED = -1;
    public const int VOTE_UNCAST = 5;
    public const int MAXPLAYERS = 64;
    //public const int VOTEINFO_CLIENT_SLOT = 0;
    //public const int VOTEINFO_CLIENT_ITEM = 1;
}

public delegate void YesNoVoteHandler(YesNoVoteAction action, int param1, int param2);
public delegate bool YesNoVoteResult(YesNoVoteInfo info);

public class CPanoramaVote
{
    private int m_iVoteCount = 0;
    private bool m_bIsVoteInProgress = false;
    private YesNoVoteHandler? m_VoteHandler = null;
    private YesNoVoteResult? m_VoteResult = null;
    private int m_iVoterCount;
    private int[] m_iVoters = new int[VoteConstants.MAXPLAYERS];
    private int m_iCurrentVoteCaller;
    private string m_szCurrentVoteTitle = string.Empty;
    private string m_szCurrentVoteDetailStr = string.Empty;
    private CVoteController? VoteController = null;
    private RecipientFilter CurrentVotefilter = new RecipientFilter();
    private BasePlugin plugin;
    public CPanoramaVote(BasePlugin basePlugin)
    {
        plugin = basePlugin;
        RegisterCommands(basePlugin);
        Reset();
    }
    

    /// <summary>
    /// Resets the vote state, clearing any ongoing vote information.
    /// </summary>
    private void Reset(CVoteController? voteController = null)
    {
        m_bIsVoteInProgress = false;
        m_VoteHandler = null;
        m_VoteResult = null;
        m_szCurrentVoteTitle = string.Empty;
        m_szCurrentVoteDetailStr = string.Empty;

        if(voteController != null) // Reset the vote controller
        {
            for (int i = 0; i < VoteConstants.MAXPLAYERS; i ++)
            {
                voteController.VotesCast[i] = (int)CastVote.VOTE_UNCAST;
            }

            voteController.VoteOptionCount[0] = 0;
            voteController.VoteOptionCount[1] = 0;
            voteController.VoteOptionCount[2] = 0;
            voteController.VoteOptionCount[3] = 0;
            voteController.VoteOptionCount[4] = 0;
        }
    }
    /// <summary>
    /// Initializes the vote controller if a vote is not already in progress.
    /// </summary>
    public void Init()
    {
        if (m_bIsVoteInProgress) return;

        CVoteController pVoteController = Utilities.FindAllEntitiesByDesignerName<CVoteController>("vote_controller").Last();

        if (pVoteController == null) return;

        VoteController = pVoteController;
    }
    /// <summary>
    /// Handles the event when a vote is cast by a player.
    /// </summary>
    /// <param name="pEvent">The game event containing vote information.</param>
    public void VoteCast(GameEvent pEvent)
    {
        if (VoteController == null || !m_bIsVoteInProgress)
            return;

        if (m_VoteHandler != null)
        {
            var pVoter = new CCSPlayerController(NativeAPI.GetEventPlayerController(pEvent.Handle,"userid"));
            if (pVoter == null) return;
            m_VoteHandler(YesNoVoteAction.VoteAction_Vote, pVoter.Slot, NativeAPI.GetEventInt(pEvent.Handle, "vote_option"));
        }
        
        UpdateVoteCounts();
        CheckForEarlyVoteClose();
    }
    /// <summary>
    /// Removes a player from the current vote.
    /// </summary>
    /// <param name="iSlot">The slot of the player to remove from the vote.</param>
    public void RemovePlayerFromVote(int iSlot)
    {
        if (!m_bIsVoteInProgress) return;

        bool found = false;
        for (int i = 0; i < m_iVoterCount; i++)
        {
            if (m_iVoters[i] == iSlot)
                found = true;
            else if (found)
                m_iVoters[i - 1] = m_iVoters[i];
        }

        if (found)
        {
            m_iVoterCount--;
            m_iVoters[m_iVoterCount] = -1;
        }
    }
    /// <summary>
    /// Checks if a player is in the current vote pool.
    /// </summary>
    /// <param name="iSlot">The slot of the player to check.</param>
    /// <returns>True if the player is in the vote pool, otherwise false.</returns>
    public bool IsPlayerInVotePool(int iSlot)
    {
        if (!m_bIsVoteInProgress)
            return false;

        if (iSlot < 0 || iSlot > m_iVoterCount)
            return false;

        return m_iVoters.Contains(iSlot);
    }
    /// <summary>
    /// Redraws the vote UI for a specific client.
    /// </summary>
    /// <param name="iSlot">The slot of the client to redraw the vote for.</param>
    /// <returns>True if the vote UI was redrawn, otherwise false.</returns>
    public bool RedrawVoteToClient(int iSlot)
    {
        if(VoteController == null)return false;

        if (!m_bIsVoteInProgress)return false;

        int myVote = VoteController.VotesCast[iSlot];
        if(myVote != (int)CastVote.VOTE_UNCAST)
        {
            VoteController.VoteOptionCount[myVote]--;
            VoteController.VotesCast[iSlot] = (int)CastVote.VOTE_UNCAST;
            UpdateVoteCounts();
        }

        SendVoteStartUM(new RecipientFilter(iSlot));

        return true;
    }
    /// <summary>
    /// Updates the vote counts and fires a vote changed event.
    /// </summary>
    public void UpdateVoteCounts()
    {
        if(VoteController == null)return;
        
        var pEventPtr = NativeAPI.CreateEvent("vote_changed", true);

        NativeAPI.SetEventInt(pEventPtr, "vote_option1", VoteController.VoteOptionCount[0]);
        NativeAPI.SetEventInt(pEventPtr, "vote_option2", VoteController.VoteOptionCount[1]);
        NativeAPI.SetEventInt(pEventPtr, "vote_option3", VoteController.VoteOptionCount[2]);
        NativeAPI.SetEventInt(pEventPtr, "vote_option4", VoteController.VoteOptionCount[3]);
        NativeAPI.SetEventInt(pEventPtr, "vote_option5", VoteController.VoteOptionCount[4]);
        NativeAPI.SetEventInt(pEventPtr, "potentialVotes", m_iVoterCount);

        NativeAPI.FireEvent(pEventPtr, false);
    }
    /// <summary>
    /// Checks if a vote is currently in progress.
    /// </summary>
    /// <returns>True if a vote is in progress, otherwise false.</returns>
    public bool IsVoteInProgress()
    {
        return m_bIsVoteInProgress;
    }
    /// <summary>
    /// Starts a new Yes/No vote for Specific Players.
    /// </summary>
    /// <param name="flDuration">Maximum time to leave the vote active for.</param>
    /// <param name="iCaller">Player slot of the vote caller. Use VOTE_CALLER_SERVER for 'Server'.</param>
    /// <param name="sVoteTitle">Translation string to use as the vote message.</param>
    /// <param name="sDetailStr">Extra string used in some vote translation strings.</param>
    /// <param name="pFilter">Recipient filter with all the clients who are allowed to participate in the vote.</param>
    /// <param name="resultCallback">Called when the vote has finished.</param>
    /// <param name="handler">Called when a menu action is completed.</param>
    /// <returns>True if the vote was successfully started, otherwise false.</returns>
    public bool SendYesNoVote(float flDuration, int iCaller, string sVoteTitle, string sDetailStr, RecipientFilter pFilter, YesNoVoteResult resultCallback, YesNoVoteHandler? handler = null)
    {
        if(VoteController == null)
            return false;

        if (m_bIsVoteInProgress)
        {
            Console.WriteLine($"[Vote Error] A vote is already in progress.");
            return false;
        }

        if (pFilter.Count <= 0)
            return false;

        if (resultCallback == null)
            return false;

        Reset(VoteController);

        // Log vote start message
        Console.WriteLine($"[Vote Start] Starting a new vote [id:{m_iVoteCount}]. Duration:{flDuration} Caller:{iCaller} NumClients:{pFilter.Count}");

        m_bIsVoteInProgress = true;
        CurrentVotefilter = pFilter;
        InitVoters(pFilter);

        VoteController.PotentialVotes = m_iVoterCount;
        VoteController.ActiveIssueIndex = 2;

        m_VoteResult = resultCallback;
        m_VoteHandler = handler;

        m_iCurrentVoteCaller = iCaller;
        m_szCurrentVoteTitle = sVoteTitle;
        m_szCurrentVoteDetailStr = sDetailStr;

        UpdateVoteCounts();
        SendVoteStartUM(pFilter);

        m_VoteHandler?.Invoke(YesNoVoteAction.VoteAction_Start, 0, 0);

        // Set up a timer to handle the end of the vote after the duration
        int voteNum = m_iVoteCount;
        new CounterStrikeSharp.API.Modules.Timers.Timer(flDuration, () =>
        {
            if (voteNum == m_iVoteCount)  // Ensure we don't end the wrong vote
                EndVote(YesNoVoteEndReason.VoteEnd_TimeUp);
        });

        return true;
    }
    /// <summary>
    /// Start a new Yes/No vote for all players
    /// </summary>
    /// <param name="flDuration">Maximum time to leave the vote active for.</param>
    /// <param name="iCaller">Player slot of the vote caller. Use VOTE_CALLER_SERVER for 'Server'.</param>
    /// <param name="sVoteTitle">Translation string to use as the vote message.</param>
    /// <param name="sDetailStr">Extra string used in some vote translation strings.</param>
    /// <param name="resultCallback">Called when the vote has finished.</param>
    /// <param name="handler">Called when a menu action is completed.</param>
    /// <returns>True if the vote was successfully started, otherwise false.</returns>
    public bool SendYesNoVoteToAll(float flDuration, int iCaller, string sVoteTitle, string sDetailStr, YesNoVoteResult resultCallback, YesNoVoteHandler? handler = null)
    {
        CurrentVotefilter.Clear();
        foreach(var player in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected))
        {
            CurrentVotefilter.Add(player);
        }

        return SendYesNoVote(flDuration, iCaller, sVoteTitle, sDetailStr, CurrentVotefilter, resultCallback, handler);
    }
    /// <summary>
    /// Sends a user message to start the vote.
    /// </summary>
    /// <param name="pFilter">Recipient filter with all the clients who are allowed to participate in the vote.</param>
    private void SendVoteStartUM(RecipientFilter pFilter)
    {
        UserMessage voteStart = UserMessage.FromId(346); //CS_UM_VoteStart = 346;
        voteStart.SetInt("team", -1);
        voteStart.SetInt("player_slot", m_iCurrentVoteCaller);
        voteStart.SetInt("vote_type", -1);
        voteStart.SetString("disp_str", m_szCurrentVoteTitle);
        voteStart.SetString("details_str", m_szCurrentVoteDetailStr);
        voteStart.SetBool("is_yes_no_vote", true);

        voteStart.Send(pFilter);
    }
    /// <summary>
    /// Initializes the voters for the current vote.
    /// </summary>
    /// <param name="pFilter">Recipient filter with all the clients who are allowed to participate in the vote.</param>
    private void InitVoters(RecipientFilter pFilter)
    {
        // Reset voter information
        m_iVoterCount = 0;
        for (int i = 0; i < VoteConstants.MAXPLAYERS; i++)
        {
            m_iVoters[i] = -1;
        }

        // Set the voters
        m_iVoterCount = pFilter.Count;
        for (int i = 0, j = 0; i < m_iVoterCount; i++)
        {
            if (pFilter[i].Slot != -1)
            {
                m_iVoters[j] = pFilter[i].Slot;
                j++;
            }
        }
    }
    /// <summary>
    /// Checks if the vote can be closed early based on the number of votes cast.
    /// </summary>
    private void CheckForEarlyVoteClose()
    {
        if(VoteController == null)return;

        int votes = VoteController.VoteOptionCount[(int)CastVote.VOTE_OPTION1] + VoteController.VoteOptionCount[(int)CastVote.VOTE_OPTION2];
        if (votes >= m_iVoterCount)
        {
            Server.NextFrame(() => EndVote(YesNoVoteEndReason.VoteEnd_AllVotes));
        }
    }
    /// <summary>
    /// Cancels the current vote.
    /// </summary>
    public void CancelVote()
    {
        if (!m_bIsVoteInProgress)
            return;

        EndVote(YesNoVoteEndReason.VoteEnd_Cancelled);
    }
    /// <summary>
    /// Ends the current vote with the specified reason.
    /// </summary>
    /// <param name="reason">The reason for ending the vote.</param>
    private void EndVote(YesNoVoteEndReason reason)
    {
        if (!m_bIsVoteInProgress)
            return;

        m_bIsVoteInProgress = false;

        switch (reason)
        {
            case YesNoVoteEndReason.VoteEnd_AllVotes:
                Console.WriteLine($"[Vote Ending] [id:{m_iVoteCount}] All possible players voted.");
                break;
            case YesNoVoteEndReason.VoteEnd_TimeUp:
                Console.WriteLine($"[Vote Ending] [id:{m_iVoteCount}] Time ran out.");
                break;
            case YesNoVoteEndReason.VoteEnd_Cancelled:
                Console.WriteLine($"[Vote Ending] [id:{m_iVoteCount}] The vote has been cancelled.");
                break;
        }

        // Cycle global vote counter
        if (m_iVoteCount == 99)
            m_iVoteCount = 0;
        else
            m_iVoteCount++;

        if (m_VoteHandler != null)
            m_VoteHandler(YesNoVoteAction.VoteAction_End, (int)reason, 0);

        if(VoteController == null)
        {
            SendVoteFailed(reason);
            return;
        }
        if (m_VoteResult == null || reason == YesNoVoteEndReason.VoteEnd_Cancelled)
        {
            SendVoteFailed(reason);
            VoteController.ActiveIssueIndex = -1;
            return;
        }
        
        YesNoVoteInfo info = new YesNoVoteInfo();
        info.num_clients = m_iVoterCount;
        info.yes_votes = VoteController!.VoteOptionCount[(int)CastVote.VOTE_OPTION1];
        info.no_votes = VoteController!.VoteOptionCount[(int)CastVote.VOTE_OPTION2];
        info.num_votes = info.yes_votes + info.no_votes;

        for (int i = 0; i < CurrentVotefilter.Count; i++)
        {
            if (i < m_iVoterCount)
            {
                //info.clientInfo[i, VOTEINFO_CLIENT_SLOT] = m_iVoters[i];
                //info.clientInfo[i, VOTEINFO_CLIENT_ITEM] = VoteController!.VotesCast[m_iVoters[i]];
                info.clientInfo[i] = (m_iVoters[i], VoteController!.VotesCast[m_iVoters[i]]);
            }
            else
            {
                //info.clientInfo[i, VOTEINFO_CLIENT_SLOT] = -1;
                //info.clientInfo[i, VOTEINFO_CLIENT_ITEM] = -1;
                info.clientInfo[i] = (-1, -1);
            }
        }

        bool passed = m_VoteResult(info);
        if (passed)
            SendVotePassed("#SFUI_vote_passed_panorama_vote", "Vote Passed!");
        else
            SendVoteFailed(reason);
    }
    /// <summary>
    /// Sends a user message indicating that the vote failed.
    /// </summary>
    private void SendVoteFailed(YesNoVoteEndReason reason)
    {
        UserMessage voteFailed = UserMessage.FromId(348); //CS_UM_VoteFailed = 348;

        voteFailed.SetInt("team", -1);
        voteFailed.SetInt("reason", (int)reason);

        RecipientFilter pFilter = new RecipientFilter();
        pFilter.AddAllPlayers();
        
        voteFailed.Send(pFilter);
        
    }
    /// <summary>
    /// Sends a user message indicating that the vote passed.
    /// </summary>
    private void SendVotePassed(string disp_str = "#SFUI_Vote_None", string details_str = "")
    {
        UserMessage votePass = UserMessage.FromId(347); //CS_UM_VotePass = 347;
        votePass.SetInt("team", -1);
        votePass.SetInt("vote_type", 2);
        votePass.SetString("disp_str", disp_str);
        votePass.SetString("details_str", details_str);

        RecipientFilter pFilter = new RecipientFilter();
        pFilter.AddAllPlayers();

        votePass.Send(pFilter);
    }
    private void RegisterCommands(BasePlugin basePlugin)
    {
        basePlugin.AddCommand("css_cancelvote", "Cancel the current vote.", OnCancelVoteCMD);
        basePlugin.AddCommand("css_revote", "Change your vote in the ongoing vote.", OnReVoteCMD);
    }
    [RequiresPermissions("@css/root")]
    public void OnCancelVoteCMD(CCSPlayerController? player, CommandInfo info) 
    {
        if(player == null || !player.IsValid)
            return;
            
        if(!m_bIsVoteInProgress)
        {
            player.PrintToChat($" {ChatColors.DarkRed}No vote is currently in progress.");
            return;
        }
        Console.WriteLine($"[Vote Cancel] Vote has been cancelled.");
        CancelVote();
        
    }
    public void OnReVoteCMD(CCSPlayerController? player, CommandInfo info) 
    {
        if(player == null || !player.IsValid)
            return;

        if(!m_bIsVoteInProgress)
        {
            player.PrintToChat($" {ChatColors.DarkRed}No vote is currently in progress.");
            return;
        }
        
        RedrawVoteToClient(player.Slot);
    }

}

