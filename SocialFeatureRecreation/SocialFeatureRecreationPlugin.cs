using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ChatShared;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using Aki.Common.Http;
using Diz.Binding;
using EFT;
using EFT.Communications;
using Newtonsoft.Json.Linq;

namespace SocialFeatureRecreation
{
    [BepInPlugin("com.nwbear.socialfeaturerecreation", "SocialFeatureRecreation", "1.0.0")]
    public class SocialFeatureRecreationPlugin : BaseUnityPlugin
    {
        public static ManualLogSource SFRLogger;

        public void Start()
        {
            SFRLogger = Logger;
            Harmony test = new Harmony("test-sfrp");
            test.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Social1), "method_14")]
    public static class IncomingFriendRequestDataPatch
    {
        public static bool Prefix(ref Result<GInvitation[]> result, Social1 __instance)
        {
            GInvitation[] invitations = result.Value;
            foreach (GInvitation invitation in invitations)
            {
                invitation.From = invitation.Profile;
            }
            __instance.InputFriendsInvitations.UpdateItems(invitations.Select(x => new Invitation(x, true)).ToArray());
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Social1), "method_15")]
    public static class OutgoingFriendRequestDataPatch
    {
        public static bool Prefix(ref Result<GInvitation[]> result, Social1 __instance)
        {
            GInvitation[] invitations = result.Value;
            foreach (GInvitation invitation in invitations)
            {
                invitation.To = invitation.Profile;
            }
            __instance.OutputFriendsInvitations.UpdateItems(invitations.Select(x => new Invitation(x, true)).ToArray());
            return false;
        }
    }

    /*
    [HarmonyPatch(typeof(Social1))]
    [HarmonyPatch("method_1")]
    public class FriendRequestDebug
    {
        private static MethodBase method_2;

        private static Social1 instance;
        
        public static void Handle(NotificationClass notif)
        {
            method_2.Invoke(instance, new object[] {notif});
        }
        
        public static bool Prefix(Social1 __instance, ISession session, ref ISession ___ginterface145_0, ref InventoryController ___gclass2755_0, ref string ___string_3, InventoryController inventoryController, string version)
        {
            ___ginterface145_0 = session;
            ___gclass2755_0 = inventoryController;
            ___string_3 = version;
            Traverse.Create(__instance).Property("PinnedDialogues").SetValue(new GClass3362<Dialogue>());
            Traverse.Create(__instance).Property("UnpinnedDialogues").SetValue(new GClass3362<Dialogue>());
            Traverse.Create(__instance).Property("List_0").SetValue(new List<Dialogue>());
            Traverse.Create(__instance).Property("SearchedFriendsList").SetValue(new GClass3362<UpdatableChatMember>());
            Traverse.Create(__instance).Property("FriendsList").SetValue(new GClass3362<UpdatableChatMember>());
            Traverse.Create(__instance).Property("IgnoringYouList").SetValue(new GClass3362<UpdatableChatMember>());
            Traverse.Create(__instance).Property("InputFriendsInvitations").SetValue(new GClass3362<Invitation>());
            Traverse.Create(__instance).Property("OutputFriendsInvitations").SetValue(new GClass3362<Invitation>());
            Traverse.Create(__instance).Property("PlayerMember").SetValue(UpdatableChatMember.FindOrCreate(___ginterface145_0.Profile.Id, (Func<string, UpdatableChatMember>)(id => new UpdatableChatMember(id))));
            __instance.PlayerMember.SetNickname(___ginterface145_0.Profile.Nickname);
            __instance.PlayerMember.SetCategory(___ginterface145_0.Profile.Info.MemberCategory);
            Traverse.Create(__instance).Property("SystemMember").SetValue(UpdatableChatMember.FindOrCreate("59e7125688a45068a6249071", (Func<string, UpdatableChatMember>)(id => new UpdatableChatMember(id))));
            __instance.SystemMember.SetNickname("SYSTEM");
            __instance.SystemMember.SetCategory(EMemberCategory.System);
            __instance.UpdateDialogueList((Action)(() =>
            {
                if (!MonoBehaviourSingleton<PreloaderUI>.Instantiated)
                    return;
                MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar.InitSocial(__instance);
            }));
            ___ginterface145_0.GetFriendsList((Callback<GClass926>)(arg =>
            {
                __instance.FriendsList.UpdateItems(
                    (ICollection<UpdatableChatMember>)((IEnumerable<UpdatableChatMember>)arg.Value.Friends).Where<UpdatableChatMember>((Func<UpdatableChatMember, bool>)(x => x != null)).ToArray<UpdatableChatMember>());
                foreach(string inIgnore in arg.Value.InIgnoreList)
                    Social1.IgnoringYouList.Add(UpdatableChatMember.FindOrCreate(inIgnore, (Func<string, UpdatableChatMember>)(id => new UpdatableChatMember(id))));
                foreach(string key in ((IEnumerable<string>)arg.Value.Ignore).Where<string>((Func<string, bool>)(item => Social1.AllMembers.ContainsKey(item))))
                    Social1.AllMembers[key].SetIgnoreStatus(true);
            }));
            //this.ginterface145_0.GetInputFriendsRequests((Callback<Invitation1[]>) (result => this.InputFriendsInvitations.UpdateItems((ICollection<Invitation>) ((IEnumerable<Invitation1>) result.Value).Select<Invitation1, Invitation>((Func<Invitation1, Invitation>) (x => new Invitation(x, true))).ToArray<Invitation>())));
            //this.ginterface145_0.GetOutputFriendsRequests((Callback<Invitation1[]>) (result => this.OutputFriendsInvitations.UpdateItems((ICollection<Invitation>) ((IEnumerable<Invitation1>) result.Value).Select<Invitation1, Invitation>((Func<Invitation1, Invitation>) (x => new Invitation(x, false))).ToArray<Invitation>())));

            JArray inboundRequestData = JArray.Parse(JObject.Parse(RequestHandler.GetJson("/client/friend/request/list/inbox", false))["data"].ToString());

            List<Invitation> invitations = new List<Invitation>();

            foreach(var a in inboundRequestData)
            {
                JObject invitation = JObject.Parse(a.ToString());
                Invitation1 inv = new Invitation1();
                inv._id = invitation["_id"].Value<string>();
                inv.From = new UpdatableChatMember(invitation["From"].Value<JObject>()["Id"].Value<string>());
                inv.From.AccountId = inv.From.Id;
                JObject fromInfo = invitation["From"].Value<JObject>()["Info"].Value<JObject>();
                inv.From.Info.Nickname = fromInfo["Nickname"].Value<string>();
                inv.From.Info.Side = fromInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
                inv.From.Info.Level = fromInfo["Level"].Value<int>();
                inv.From.Info.MemberCategory = (EMemberCategory)(int)fromInfo["MemberCategory"].Value<Int64>();
                inv.From.Info.Ignored = fromInfo["Ignored"].Value<bool>();
                inv.From.Info.Banned = fromInfo["Banned"].Value<bool>();
                inv.To = new UpdatableChatMember(invitation["To"].Value<JObject>()["Id"].Value<string>());
                inv.To.AccountId = inv.To.Id;
                JObject toInfo = invitation["To"].Value<JObject>()["Info"].Value<JObject>();
                inv.To.Info.Nickname = toInfo["Nickname"].Value<string>();
                inv.To.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
                inv.To.Info.Level = toInfo["Level"].Value<int>();
                inv.To.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
                inv.To.Info.Ignored = toInfo["Ignored"].Value<bool>();
                inv.To.Info.Banned = toInfo["Banned"].Value<bool>();
                inv.Profile = inv.From;
                Invitation finalInv = new Invitation(inv, true);
                invitations.Add(finalInv);
            }

            __instance.InputFriendsInvitations.UpdateItems(invitations);

            JArray outboundRequestData = JArray.Parse(JObject.Parse(RequestHandler.GetJson("/client/friend/request/list/outbox", false))["data"].ToString());

            List<Invitation> invitationsTwo = new List<Invitation>();

            foreach(var a in outboundRequestData)
            {
                JObject invitation = JObject.Parse(a.ToString());
                Invitation1 inv = new Invitation1();
                inv._id = invitation["_id"].Value<string>();
                inv.From = new UpdatableChatMember(invitation["From"].Value<JObject>()["Id"].Value<string>());
                inv.From.AccountId = inv.From.Id;
                JObject fromInfo = invitation["From"].Value<JObject>()["Info"].Value<JObject>();
                inv.From.Info.Nickname = fromInfo["Nickname"].Value<string>();
                inv.From.Info.Side = fromInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
                inv.From.Info.Level = fromInfo["Level"].Value<int>();
                inv.From.Info.MemberCategory = (EMemberCategory)(int)fromInfo["MemberCategory"].Value<Int64>();
                inv.From.Info.Ignored = fromInfo["Ignored"].Value<bool>();
                inv.From.Info.Banned = fromInfo["Banned"].Value<bool>();
                inv.To = new UpdatableChatMember(invitation["To"].Value<JObject>()["Id"].Value<string>());
                inv.To.AccountId = inv.To.Id;
                JObject toInfo = invitation["To"].Value<JObject>()["Info"].Value<JObject>();
                inv.To.Info.Nickname = toInfo["Nickname"].Value<string>();
                inv.To.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
                inv.To.Info.Level = toInfo["Level"].Value<int>();
                inv.To.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
                inv.To.Info.Ignored = toInfo["Ignored"].Value<bool>();
                inv.To.Info.Banned = toInfo["Banned"].Value<bool>();
                inv.Profile = inv.To;
                Invitation finalInv = new Invitation(inv, false);
                invitationsTwo.Add(finalInv);
            }

            __instance.OutputFriendsInvitations.UpdateItems(invitationsTwo);

            method_2 = typeof(Social1).GetMethod("method_2", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            instance = __instance;
            
            Singleton<NotificationManagerClass>.Instance.OnNotificationReceived -= new Action<NotificationClass>(Handle);
            Singleton<NotificationManagerClass>.Instance.OnNotificationReceived += new Action<NotificationClass>(Handle);
            return false;
        }
    }

    [HarmonyPatch(typeof(GClass2040))]
    [HarmonyPatch(nameof(GClass2040.ParseNotificationByType))]
    public class FriendRequestNotificationDebug
    {
        public static NotificationClass Parse(UnparsedData data)
        {
            JObject newFriend = data.JObject;
            NewFriend newFriendInstance = new NewFriend();
            newFriendInstance.Id = newFriend["_id"].Value<string>();
            newFriendInstance.Profile = new UpdatableChatMember(newFriend["profile"].Value<JObject>()["Id"].Value<string>());
            newFriendInstance.Profile.AccountId = newFriendInstance.Profile.Id;
            JObject toInfo = newFriend["profile"].Value<JObject>()["Info"].Value<JObject>();
            newFriendInstance.Profile.Info.Nickname = toInfo["Nickname"].Value<string>();
            newFriendInstance.Profile.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
            newFriendInstance.Profile.Info.Level = toInfo["Level"].Value<int>();
            newFriendInstance.Profile.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
            newFriendInstance.Profile.Info.Ignored = toInfo["Ignored"].Value<bool>();
            newFriendInstance.Profile.Info.Banned = toInfo["Banned"].Value<bool>();
            return newFriendInstance;
        }
        
        public static NotificationClass ParseCanceled(UnparsedData data)
        {
            JObject newFriend = data.JObject;
            FriendRequestCanceled newFriendInstance = new FriendRequestCanceled();
            newFriendInstance.Profile = new UpdatableChatMember(newFriend["profile"].Value<JObject>()["Id"].Value<string>());
            newFriendInstance.Profile.AccountId = newFriendInstance.Profile.Id;
            JObject toInfo = newFriend["profile"].Value<JObject>()["Info"].Value<JObject>();
            newFriendInstance.Profile.Info.Nickname = toInfo["Nickname"].Value<string>();
            newFriendInstance.Profile.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
            newFriendInstance.Profile.Info.Level = toInfo["Level"].Value<int>();
            newFriendInstance.Profile.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
            newFriendInstance.Profile.Info.Ignored = toInfo["Ignored"].Value<bool>();
            newFriendInstance.Profile.Info.Banned = toInfo["Banned"].Value<bool>();
            return newFriendInstance;
        }
        
        public static NotificationClass ParseDeleted(UnparsedData data)
        {
            JObject newFriend = data.JObject;
            AbstractNotification15 newFriendInstance = new AbstractNotification15();
            newFriendInstance.Profile = new UpdatableChatMember(newFriend["profile"].Value<JObject>()["Id"].Value<string>());
            newFriendInstance.Profile.AccountId = newFriendInstance.Profile.Id;
            JObject toInfo = newFriend["profile"].Value<JObject>()["Info"].Value<JObject>();
            newFriendInstance.Profile.Info.Nickname = toInfo["Nickname"].Value<string>();
            newFriendInstance.Profile.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
            newFriendInstance.Profile.Info.Level = toInfo["Level"].Value<int>();
            newFriendInstance.Profile.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
            newFriendInstance.Profile.Info.Ignored = toInfo["Ignored"].Value<bool>();
            newFriendInstance.Profile.Info.Banned = toInfo["Banned"].Value<bool>();
            return newFriendInstance;
        }
        
        public static NotificationClass ParseDeclined(UnparsedData data)
        {
            JObject newFriend = data.JObject;
            AbstractNotification21 newFriendInstance = new AbstractNotification21();
            newFriendInstance.Profile = new UpdatableChatMember(newFriend["profile"].Value<JObject>()["Id"].Value<string>());
            newFriendInstance.Profile.AccountId = newFriendInstance.Profile.Id;
            JObject toInfo = newFriend["profile"].Value<JObject>()["Info"].Value<JObject>();
            newFriendInstance.Profile.Info.Nickname = toInfo["Nickname"].Value<string>();
            newFriendInstance.Profile.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
            newFriendInstance.Profile.Info.Level = toInfo["Level"].Value<int>();
            newFriendInstance.Profile.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
            newFriendInstance.Profile.Info.Ignored = toInfo["Ignored"].Value<bool>();
            newFriendInstance.Profile.Info.Banned = toInfo["Banned"].Value<bool>();
            return newFriendInstance;
        }
        
        public static NotificationClass ParseAccepted(UnparsedData data)
                {
                    JObject newFriend = data.JObject;
                    AbstractNotification17 newFriendInstance = new AbstractNotification17();
                    newFriendInstance.Profile = new UpdatableChatMember(newFriend["profile"].Value<JObject>()["Id"].Value<string>());
                    newFriendInstance.Profile.AccountId = newFriendInstance.Profile.Id;
                    JObject toInfo = newFriend["profile"].Value<JObject>()["Info"].Value<JObject>();
                    newFriendInstance.Profile.Info.Nickname = toInfo["Nickname"].Value<string>();
                    newFriendInstance.Profile.Info.Side = toInfo["Side"].Value<string>() == "Usec" ? EChatMemberSide.Usec : EChatMemberSide.Bear;
                    newFriendInstance.Profile.Info.Level = toInfo["Level"].Value<int>();
                    newFriendInstance.Profile.Info.MemberCategory = (EMemberCategory)(int)toInfo["MemberCategory"].Value<Int64>();
                    newFriendInstance.Profile.Info.Ignored = toInfo["Ignored"].Value<bool>();
                    newFriendInstance.Profile.Info.Banned = toInfo["Banned"].Value<bool>();
                    return newFriendInstance;
                }

        public static bool Prefix(ENotificationType type, UnparsedData data, ref NotificationClass __result)
        {   
            NotificationClass notificationByType = (NotificationClass)null;
            switch (type)
            {
                case ENotificationType.Ping:
                case ENotificationType.ChannelDeleted:
                    GClass2040.Logger.LogInfo(string.Format("Got notification | {0}\n{1}", (object)type, (object)data.JToken));
                    __result = notificationByType;
                    return false;
                case ENotificationType.TraderSupply:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification6>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchInviteAccept:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification10>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchInviteDecline:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification11>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchWasRemoved:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification25>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchInviteSend:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification7>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchInviteCancel:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification8>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchInviteExpired:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification9>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchLeaderChanged:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification24>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchUserLeave:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification12>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMaxCountReached:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification13>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchStartGame:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification22>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchRaidSettings:
                    notificationByType = (NotificationClass)data.ParseJsonTo<NotificationMatchRaidSettings>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchRaidReady:
                    notificationByType = (NotificationClass)data.ParseJsonTo<NotificationGroupMatchRaidReady>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchRaidNotReady:
                    notificationByType = (NotificationClass)data.ParseJsonTo<NotificationGroupMatchRaidNotReady>();
                    goto case ENotificationType.Ping;
                case ENotificationType.GroupMatchAbort:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification23>();
                    goto case ENotificationType.Ping;
                case ENotificationType.WrongMajorVersion:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification3>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ChatMessageReceived:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification14>();
                    goto case ENotificationType.Ping;
                case ENotificationType.RemovedFromFriendsList:
                    notificationByType = (NotificationClass)ParseDeleted(data);
                    goto case ENotificationType.Ping;
                case ENotificationType.FriendsListNewRequest:
                    notificationByType = (NotificationClass)Parse(data);
                    goto case ENotificationType.Ping;
                case ENotificationType.FriendsListRequestCanceled:
                    notificationByType = (NotificationClass)ParseCanceled(data);
                    goto case ENotificationType.Ping;
                case ENotificationType.TournamentWarning:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification5>();
                    goto case ENotificationType.Ping;
                case ENotificationType.FriendsListDecline:
                    notificationByType = (NotificationClass)ParseDeclined(data);
                    goto case ENotificationType.Ping;
                case ENotificationType.FriendsListAccept:
                    notificationByType = (NotificationClass)ParseAccepted(data);
                    goto case ENotificationType.Ping;
                case ENotificationType.YouWasKickedFromDialogue:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification18>();
                    goto case ENotificationType.Ping;
                case ENotificationType.YouWereAddedToIgnoreList:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification19>();
                    goto case ENotificationType.Ping;
                case ENotificationType.YouWereRemovedToIgnoreList:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification20>();
                    goto case ENotificationType.Ping;
                case ENotificationType.RagfairOfferSold:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass2008>();
                    goto case ENotificationType.Ping;
                case ENotificationType.RagfairRatingChange:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1971>();
                    goto case ENotificationType.Ping;
                case ENotificationType.RagfairNewRating:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass2007>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ForceLogout:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification27>();
                    goto case ENotificationType.Ping;
                case ENotificationType.InGameBan:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification28>();
                    goto case ENotificationType.Ping;
                case ENotificationType.InGameUnBan:
                    notificationByType = (NotificationClass)data.ParseJsonTo<AbstractNotification29>();
                    goto case ENotificationType.Ping;
                case ENotificationType.TraderStanding:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1974>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ProfileLevel:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1961>();
                    goto case ENotificationType.Ping;
                case ENotificationType.SkillPoints:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1963>();
                    goto case ENotificationType.Ping;
                case ENotificationType.HideoutAreaLevel:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1967>();
                    goto case ENotificationType.Ping;
                case ENotificationType.AssortmentUnlockRule:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1969>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ExamineItems:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1968>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ExamineAllItems:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1970>();
                    goto case ENotificationType.Ping;
                case ENotificationType.TraderSalesSum:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1976>();
                    goto case ENotificationType.Ping;
                case ENotificationType.UnlockTrader:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1972>();
                    goto case ENotificationType.Ping;
                case ENotificationType.StashRows:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1959>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ProfileLockTimer:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1960>();
                    goto case ENotificationType.Ping;
                case ENotificationType.MasteringSkill:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1965>();
                    goto case ENotificationType.Ping;
                case ENotificationType.ProfileExperienceDelta:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1962>();
                    goto case ENotificationType.Ping;
                case ENotificationType.TraderStandingDelta:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1975>();
                    goto case ENotificationType.Ping;
                case ENotificationType.TraderSalesSumDelta:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1977>();
                    goto case ENotificationType.Ping;
                case ENotificationType.SkillPointsDelta:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1964>();
                    goto case ENotificationType.Ping;
                case ENotificationType.MasteringSkillDelta:
                    notificationByType = (NotificationClass)data.ParseJsonTo<GClass1966>();
                    goto case ENotificationType.Ping;
                case ENotificationType.UserMatched:
                case ENotificationType.UserMatchOver:
                case ENotificationType.UserConfirmed:
                    notificationByType = (NotificationClass)new AbstractNotification4()
                    {
                        Status = data.ParseJsonTo<Status1>()
                    };
                    goto case ENotificationType.Ping;
                default:
                    GClass2040.Logger.LogError("Not specified type for notification: {0}", (object)type);
                    goto case ENotificationType.Ping;
            }
        }
    }

    [HarmonyPatch(typeof(Social1.Class1341))]
    [HarmonyPatch("method_1")]
    public class PatchDeclineRemoval
    {
        public static bool Prefix(Social1.Class1341 __instance, Invitation x, ref bool __result)
        {
            __result = x.To.Id == Traverse.Create(__instance).Field("<friendsDecline>5__3").GetValue<AbstractNotification21>().Profile.Id;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Social1.Class1341))]
    [HarmonyPatch("method_0")]
    public class PatchAcceptRemoval
    {
        public static bool Prefix(Social1.Class1341 __instance, Invitation x, ref bool __result)
        {
            __result = x.To.Id == Traverse.Create(__instance).Field("<friendsAccept>5__2;").GetValue<AbstractNotification17>().Profile.Id;
            return false;
        }
    }

    [HarmonyPatch(typeof(Social1))]
    [HarmonyPatch("method_2")]
    public class PatchFriendDeletion
    {
        public static bool Prefix(Social1 __instance, NotificationClass notification)
        {
            Social1.Class1341 class1341 = new Social1.Class1341();
            switch (notification)
            {
                case AbstractNotification14 abstractNotification14 when abstractNotification14.Message != null:
                    MethodBase method_6 = typeof(Social1).GetMethod("method_6",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    method_6.Invoke(__instance, new object[] {
                    abstractNotification14.Message, abstractNotification14.DialogueId
                });
                    break;
                case AbstractNotification15 abstractNotification15:
                  __instance.FriendsList.RemoveFirst((friend) => friend.Id == abstractNotification15.Profile.Id);
                Singleton<GUISounds>.Instance.PlayChatSound(ESocialNetworkSoundType.DeclinedRequest);
                break;
              case AbstractNotification17 abstractNotification17:
                SocialFeatureRecreationPlugin.SFRLogger.LogInfo("Accepted: " + abstractNotification17.Profile.Id);
                __instance.FriendsList.Add(abstractNotification17.Profile);
                __instance.OutputFriendsInvitations.RemoveFirst((invitation) => invitation.To.Id == abstractNotification17.Profile.Id);
                Singleton<GUISounds>.Instance.PlayChatSound(ESocialNetworkSoundType.AcceptedRequest);
                break;
              case AbstractNotification21 abstractNotification21:
                __instance.OutputFriendsInvitations.RemoveFirst((invitation) => invitation.To.Id == abstractNotification21.Profile.Id);
                Singleton<GUISounds>.Instance.PlayChatSound(ESocialNetworkSoundType.DeclinedRequest);
                break;
              case AbstractNotification18 abstractNotification18:
                // ISSUE: reference to a compiler-generated field
                Traverse.Create(__instance).Field("<kickedFromGroup>5__4").SetValue(abstractNotification18);
                // ISSUE: reference to a compiler-generated method
                MethodBase method_2 = class1341.GetType()
                    .GetMethod("method_2",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                __instance.RemoveDialogue(__instance.Dialogues.FirstOrDefault<Dialogue>(new Func<Dialogue, bool>((diag) => { return (bool)method_2.Invoke(__instance, new object[] { diag }); })));
                break;
              case AbstractNotification19 abstractNotification19:
                Social1.IgnoringYouList.Add(abstractNotification19.Profile);
                Singleton<GUISounds>.Instance.PlayChatSound(ESocialNetworkSoundType.StartIgnore);
                break;
              case AbstractNotification20 abstractNotification20:
                Social1.IgnoringYouList.Remove(abstractNotification20.Profile);
                break;
              case NewFriend newFriend:
                MethodBase method_3 = typeof(Social1).GetMethod("method_3",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method_3.Invoke(__instance, new object[] {newFriend});
                break;
              case FriendRequestCanceled friendRequestCanceled:
                    MethodBase method_4 = typeof(Social1).GetMethod("method_4",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                method_4.Invoke(__instance, new object[] {friendRequestCanceled});
                break;
            }
            return false;
        }
    }*/
}