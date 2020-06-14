using System.Collections.Generic;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace smIRCL
{
    public static class Numerics
    {
        //Command Responses
        public const string RPL_WELCOME = "001";
        public const string RPL_YOURHOST = "002";
        public const string RPL_CREATED = "003";
        public const string RPL_MYINFO = "004";
        public const string RPL_BOUNCE = "005";
        public const string RPL_AWAY = "301";
        public const string RPL_USERHOST = "302";
        public const string RPL_ISON = "303";
        public const string RPL_UNAWAY = "305";
        public const string RPL_NOWAWAY = "306";
        public const string RPL_WHOISUSER = "311";
        public const string RPL_WHOISSERVER = "312";
        public const string RPL_WHOISOPERATOR = "313";
        public const string RPL_WHOWASUSER = "314";
        public const string RPL_ENDOFWHO = "315";
        public const string RPL_WHOISIDLE = "317";
        public const string RPL_ENDOFWHOIS = "318";
        public const string RPL_WHOISCHANNELS = "319";
        public const string RPL_RPL_LISTSTART = "321"; //RFC2819: Obsolete. Not used.
        public const string RPL_LIST = "322";
        public const string RPL_LISTEND = "323";
        public const string RPL_CHANNELMODEIS = "324";
        public const string RPL_UNIQOPIS = "325";
        public const string RPL_NOTOPIC = "331";
        public const string RPL_TOPIC = "332";
        public const string RPL_INVITING = "341";
        public const string RPL_SUMMONING = "342";
        public const string RPL_INVITELIST = "346";
        public const string RPL_ENDOFINVITELIST = "347";
        public const string RPL_EXCEPTLIST = "348";
        public const string RPL_ENDOFEXCEPTLIST = "349";
        public const string RPL_VERSION = "351";
        public const string RPL_WHOREPLY = "352";
        public const string RPL_NAMREPLY = "353";
        public const string RPL_LINKS = "364";
        public const string RPL_ENDOFLINKS = "365";
        public const string RPL_ENDOFNAMES = "366";
        public const string RPL_BANLIST = "367";
        public const string RPL_ENDOFBANLIST = "368";
        public const string RPL_ENDOFWHOWAS = "369";
        public const string RPL_INFO = "371";
        public const string RPL_MOTD = "372";
        public const string RPL_ENDOFINFO = "374";
        public const string RPL_MOTDSTART = "375";
        public const string RPL_ENDOFMOTD = "376";
        public const string RPL_YOUREOPER = "381";
        public const string RPL_REHASHING = "382";
        public const string RPL_YOURESERVICE = "383";
        public const string RPL_TIME = "391";
        public const string RPL_USERSSTART = "392";
        public const string RPL_USERS = "393";
        public const string RPL_ENDOFUSERS = "394";
        public const string RPL_NOUSERS = "395";
        public const string RPL_TRACELINK = "200";
        public const string RPL_TRACECONNECTING = "201";
        public const string RPL_TRACEHANDSHAKE = "202";
        public const string RPL_TRACEUNKNOWN = "203";
        public const string RPL_TRACEOPERATOR = "204";
        public const string RPL_TRACEUSER = "205";
        public const string RPL_TRACESERVER = "206";
        public const string RPL_TRACENEWTYPE = "208";
        public const string RPL_TRACECLASS = "209";
        public const string RPL_TRACERECONNECT = "210";
        public const string RPL_STATSLINKINFO = "211";
        public const string RPL_STATSCOMMANDS = "212";
        public const string RPL_ENDOFSTATS = "219";
        public const string RPL_UMODEIS = "221";
        public const string RPL_SERVLIST = "234";
        public const string RPL_SERVLISTEND = "235";
        public const string RPL_STATSUPTIME = "242";
        public const string RPL_STATSOLINE = "243";
        public const string RPL_LUSERCLIENT = "251";
        public const string RPL_LUSEROP = "252";
        public const string RPL_LUSERUNKNOWN = "253";
        public const string RPL_LUSERCHANNELS = "254";
        public const string RPL_LUSERME = "255";
        public const string RPL_ADMINME = "256";
        public const string RPL_ADMINLOC1 = "257";
        public const string RPL_ADMINLOC2 = "258";
        public const string RPL_ADMINEMAIL = "259";
        public const string RPL_TRACELOG = "261";
        public const string RPL_TRACEEND = "262";
        public const string RPL_TRYAGAIN = "263";

        //Error Replies
        public const string ERR_NOSUCHNICK = "401";
        public const string ERR_NOSUCHSERVER = "402";
        public const string ERR_NOSUCHCHANNEL = "403";
        public const string ERR_CANNOTSENDTOCHAN = "404";
        public const string ERR_TOOMANYCHANNELS = "405";
        public const string ERR_WASNOSUCHNICK = "406";
        public const string ERR_TOOMANYTARGETS = "407";
        public const string ERR_NOSUCHSERVICE = "408";
        public const string ERR_NOORIGIN = "409";
        public const string ERR_NORECIPIENT = "411";
        public const string ERR_NOTEXTTOSEND = "412";
        public const string ERR_NOTOPLEVEL = "413";
        public const string ERR_WILDTOPLEVEL = "414";
        public const string ERR_BADMASK = "415";
        public const string ERR_UNKNOWNCOMMAND = "421";
        public const string ERR_NOMOTD = "422";
        public const string ERR_NOADMININFO = "423";
        public const string ERR_FILEERROR = "424";
        public const string ERR_NONICKNAMEGIVEN = "431";
        public const string ERR_ERRONEUSNICKNAME = "432";
        public const string ERR_NICKNAMEINUSE = "433";
        public const string ERR_NICKCOLLISION = "436";
        public const string ERR_UNAVAILRESOURCE = "437";
        public const string ERR_USERNOTINCHANNEL = "441";
        public const string ERR_NOTINCHANNEL = "442";
        public const string ERR_USERONCHANNEL = "443";
        public const string ERR_NOLOGIN = "444";
        public const string ERR_SUMMONDISABLED = "445";
        public const string ERR_USERDISABLED = "446";
        public const string ERR_NOTREGISTERED = "451";
        public const string ERR_NEEDMOREPARAMS = "461";
        public const string ERR_ALREADYREGISTERED = "462";
        public const string ERR_NOPERMFORHOST = "463";
        public const string ERR_PASSWDMISMATCH = "464";
        public const string ERR_YOUREBANNEDCREEP = "465";
        public const string ERR_YOUWILLBEBANNED = "466";
        public const string ERR_KEYSET = "467";
        public const string ERR_CHANNELISFULL = "471";
        public const string ERR_UNKNOWNMODE = "472";
        public const string ERR_INVITEONLYCHAN = "473";
        public const string ERR_BANNEDFROMCHAN = "474";
        public const string ERR_BADCHANNELKEY = "475";
        public const string ERR_BADCHANMASK = "476";
        public const string ERR_NOCHANMODES = "477";
        public const string ERR_BANLISTFULL = "478";
        public const string ERR_NOPRIVILEGES = "481";
        public const string ERR_CHANOPRIVSNEEDED = "482";
        public const string ERR_CANTKILLSERVER = "483";
        public const string ERR_RESTRICTED = "484";
        public const string ERR_UNIQOPPRIVSNEEDED = "485";
        public const string ERR_NOOPERHOST = "491";
        public const string ERR_UMODEUNKNOWNFLAG = "501";
        public const string ERR_USERSDONTMATCH = "502";

        //Reserved numerics
        //1. no longer in use
        //2. reserved for future planned use
        //3. in current use but are part of a non-generic 'feature' of the current IRC server
        public const string RPL_STATSCLINE = "213";
        public const string RPL_STATSNLINE = "214";
        public const string RPL_STATSILINE = "215";
        public const string RPL_STATSKLINE = "216";
        public const string RPL_STATSQLINE = "217";
        public const string RPL_STATSYLINE = "218";
        public const string RPL_SERVICEINFO = "231";
        public const string RPL_ENDOFSERVICES = "232";
        public const string RPL_SERVICE = "233";
        public const string RPL_STATSVLINE = "240";
        public const string RPL_STATSLLINE = "241";
        public const string RPL_STATSHLINE = "244"; //244 CONFLICT
        public const string RPL_STATSSLINE = "244"; //244 CONFLICT
        public const string RPL_STATSPING = "246";
        public const string RPL_STATSBLINE = "247";
        public const string RPL_STATSDLINE = "250";
        public const string RPL_NONE = "300";
        public const string RPL_KILLDONE = "361";
        public const string RPL_CLOSING = "362";
        public const string RPL_CLODEEND = "363";
        public const string RPL_INFOSTART = "373";
        public const string RPL_MYPORTIS = "384";
        public const string ERR_NOSERVICEHOST = "492";




        //Mapping
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            {"RPL_WELCOME", "001"},
            {"RPL_YOURHOST", "002"},
            {"RPL_CREATED", "003"},
            {"RPL_MYINFO", "004"},
            {"RPL_BOUNCE", "005"},
            {"RPL_AWAY", "301"},
            {"RPL_USERHOST", "302"},
            {"RPL_ISON", "303"},
            {"RPL_UNAWAY", "305"},
            {"RPL_NOWAWAY", "306"},
            {"RPL_WHOISUSER", "311"},
            {"RPL_WHOISSERVER", "312"},
            {"RPL_WHOISOPERATOR", "313"},
            {"RPL_WHOWASUSER", "314"},
            {"RPL_ENDOFWHO", "315"},
            {"RPL_WHOISIDLE", "317"},
            {"RPL_ENDOFWHOIS", "318"},
            {"RPL_WHOISCHANNELS", "319"},
            {"RPL_RPL_LISTSTART", "321"}, //RFC2819: Obsolete. Not used.
            {"RPL_LIST", "322"},
            {"RPL_LISTEND", "323"},
            {"RPL_CHANNELMODEIS", "324"},
            {"RPL_UNIQOPIS", "325"},
            {"RPL_NOTOPIC", "331"},
            {"RPL_TOPIC", "332"},
            {"RPL_INVITING", "341"},
            {"RPL_SUMMONING", "342"},
            {"RPL_INVITELIST", "346"},
            {"RPL_ENDOFINVITELIST", "347"},
            {"RPL_EXCEPTLIST", "348"},
            {"RPL_ENDOFEXCEPTLIST", "349"},
            {"RPL_VERSION", "351"},
            {"RPL_WHOREPLY", "352"},
            {"RPL_NAMREPLY", "353"},
            {"RPL_LINKS", "364"},
            {"RPL_ENDOFLINKS", "365"},
            {"RPL_ENDOFNAMES", "366"},
            {"RPL_BANLIST", "367"},
            {"RPL_ENDOFBANLIST", "368"},
            {"RPL_ENDOFWHOWAS", "369"},
            {"RPL_INFO", "371"},
            {"RPL_MOTD", "372"},
            {"RPL_ENDOFINFO", "374"},
            {"RPL_MOTDSTART", "375"},
            {"RPL_ENDOFMOTD", "376"},
            {"RPL_YOUREOPER", "381"},
            {"RPL_REHASHING", "382"},
            {"RPL_YOURESERVICE", "383"},
            {"RPL_TIME", "391"},
            {"RPL_USERSSTART", "392"},
            {"RPL_USERS", "393"},
            {"RPL_ENDOFUSERS", "394"},
            {"RPL_NOUSERS", "395"},
            {"RPL_TRACELINK", "200"},
            {"RPL_TRACECONNECTING", "201"},
            {"RPL_TRACEHANDSHAKE", "202"},
            {"RPL_TRACEUNKNOWN", "203"},
            {"RPL_TRACEOPERATOR", "204"},
            {"RPL_TRACEUSER", "205"},
            {"RPL_TRACESERVER", "206"},
            {"RPL_TRACENEWTYPE", "208"},
            {"RPL_TRACECLASS", "209"},
            {"RPL_TRACERECONNECT", "210"},
            {"RPL_STATSLINKINFO", "211"},
            {"RPL_STATSCOMMANDS", "212"},
            {"RPL_ENDOFSTATS", "219"},
            {"RPL_UMODEIS", "221"},
            {"RPL_SERVLIST", "234"},
            {"RPL_SERVLISTEND", "235"},
            {"RPL_STATSUPTIME", "242"},
            {"RPL_STATSOLINE", "243"},
            {"RPL_LUSERCLIENT", "251"},
            {"RPL_LUSEROP", "252"},
            {"RPL_LUSERUNKNOWN", "253"},
            {"RPL_LUSERCHANNELS", "254"},
            {"RPL_LUSERME", "255"},
            {"RPL_ADMINME", "256"},
            {"RPL_ADMINLOC1", "257"},
            {"RPL_ADMINLOC2", "258"},
            {"RPL_ADMINEMAIL", "259"},
            {"RPL_TRACELOG", "261"},
            {"RPL_TRACEEND", "262"},
            {"RPL_TRYAGAIN", "263"},
            {"ERR_NOSUCHNICK", "401"},
            {"ERR_NOSUCHSERVER", "402"},
            {"ERR_NOSUCHCHANNEL", "403"},
            {"ERR_CANNOTSENDTOCHAN", "404"},
            {"ERR_TOOMANYCHANNELS", "405"},
            {"ERR_WASNOSUCHNICK", "406"},
            {"ERR_TOOMANYTARGETS", "407"},
            {"ERR_NOSUCHSERVICE", "408"},
            {"ERR_NOORIGIN", "409"},
            {"ERR_NORECIPIENT", "411"},
            {"ERR_NOTEXTTOSEND", "412"},
            {"ERR_NOTOPLEVEL", "413"},
            {"ERR_WILDTOPLEVEL", "414"},
            {"ERR_BADMASK", "415"},
            {"ERR_UNKNOWNCOMMAND", "421"},
            {"ERR_NOMOTD", "422"},
            {"ERR_NOADMININFO", "423"},
            {"ERR_FILEERROR", "424"},
            {"ERR_NONICKNAMEGIVEN", "431"},
            {"ERR_ERRONEUSNICKNAME", "432"},
            {"ERR_NICKNAMEINUSE", "433"},
            {"ERR_NICKCOLLISION", "436"},
            {"ERR_UNAVAILRESOURCE", "437"},
            {"ERR_USERNOTINCHANNEL", "441"},
            {"ERR_NOTINCHANNEL", "442"},
            {"ERR_USERONCHANNEL", "443"},
            {"ERR_NOLOGIN", "444"},
            {"ERR_SUMMONDISABLED", "445"},
            {"ERR_USERDISABLED", "446"},
            {"ERR_NOTREGISTERED", "451"},
            {"ERR_NEEDMOREPARAMS", "461"},
            {"ERR_ALREADYREGISTERED", "462"},
            {"ERR_NOPERMFORHOST", "463"},
            {"ERR_PASSWDMISMATCH", "464"},
            {"ERR_YOUREBANNEDCREEP", "465"},
            {"ERR_YOUWILLBEBANNED", "466"},
            {"ERR_KEYSET", "467"},
            {"ERR_CHANNELISFULL", "471"},
            {"ERR_UNKNOWNMODE", "472"},
            {"ERR_INVITEONLYCHAN", "473"},
            {"ERR_BANNEDFROMCHAN", "474"},
            {"ERR_BADCHANNELKEY", "475"},
            {"ERR_BADCHANMASK", "476"},
            {"ERR_NOCHANMODES", "477"},
            {"ERR_BANLISTFULL", "478"},
            {"ERR_NOPRIVILEGES", "481"},
            {"ERR_CHANOPRIVSNEEDED", "482"},
            {"ERR_CANTKILLSERVER", "483"},
            {"ERR_RESTRICTED", "484"},
            {"ERR_UNIQOPPRIVSNEEDED", "485"},
            {"ERR_NOOPERHOST", "491"},
            {"ERR_UMODEUNKNOWNFLAG", "501"},
            {"ERR_USERSDONTMATCH", "502"},
            {"RPL_STATSCLINE", "213"},
            {"RPL_STATSNLINE", "214"},
            {"RPL_STATSILINE", "215"},
            {"RPL_STATSKLINE", "216"},
            {"RPL_STATSQLINE", "217"},
            {"RPL_STATSYLINE", "218"},
            {"RPL_SERVICEINFO", "231"},
            {"RPL_ENDOFSERVICES", "232"},
            {"RPL_SERVICE", "233"},
            {"RPL_STATSVLINE", "240"},
            {"RPL_STATSLLINE", "241"},
            {"RPL_STATSHLINE", "244"}, //244 CONFLICT
            {"RPL_STATSSLINE", "244"}, //244 CONFLICT
            {"RPL_STATSPING", "246"},
            {"RPL_STATSBLINE", "247"},
            {"RPL_STATSDLINE", "250"},
            {"RPL_NONE", "300"},
            {"RPL_KILLDONE", "361"},
            {"RPL_CLOSING", "362"},
            {"RPL_CLODEEND", "363"},
            {"RPL_INFOSTART", "373"},
            {"RPL_MYPORTIS", "384"},
            {"ERR_NOSERVICEHOST", "492"}
        };
    }
}
