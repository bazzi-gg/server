namespace Server.Helpers
{
    public static class ChannelString
    {
        public static string ChannelToMatchType(string channel)
        {
            // tierMatching_speedTeam는 스피드 매우빠름 등급전(팀전)을 의미함
            if (channel.Contains("speedTeam") || channel.StartsWith("tierMatching_speedTeam"))
            {
                return "effd66758144a29868663aa50e85d3d95c5bc0147d7fdb9802691c2087f3416e";
            }
            // tierMatching_speedIndi는 스피드 매우빠름 등급전(개인전)을 의미함
            else if (channel.Contains("speedIndi") || channel.StartsWith("tierMatching_speedIndi"))
            {
                return "7b9f0fd5377c38514dbb78ebe63ac6c3b81009d5a31dd569d1cff8f005aa881a";
            }
            // tierMatching_itemNew는 tierMatching_itemNewItemTeam, 아이템 가장빠름 등급전(팀전)을 의미함
            else if (channel.Contains("itemNewItemTeam") || channel.StartsWith("tierMatching_itemNew") || channel.StartsWith("itemTeam"))
            {
                return "14e772d195642279cf6c8307125044274db371c1b08fc3dd6553e50d76d2b3aa";
            }
            // grandprix_itemNew는 grandprix_itemNewItemIndi, 아이템 개인전 빠름을 의미함
            else if (channel.Contains("itemNewItemIndi") || channel.StartsWith("grandprix_itemNew") || channel.StartsWith("itemIndi"))
            {
                return "7ca6fd44026a2c8f5d939b60aa56b4b1714b9cc2355ec5e317154d4cf0675da0";
            }
            else if (channel == "clubRace_speed")
            {
                return "826ecdb309f3a2b80a790902d1b133499866d6b933c7deb0916979d1232f968c";
            }
            else if (channel == "clubRace_item")
            {
                return "e7be8820e2836e5779dfb5339956768c04ea6cc5788babb1e993b764b86ccec8";
            }
            else if (channel == "battle")
            {
                return "ee2426e23fa56f7a695084e1fc07fe6bb03a0b3b0c71c4e1f1b7e7e78e6c6878";
            }
            else if (channel.Contains("bokbulbokSpeedIndi"))
            {
                return "b73122a1e6559949df183992491d440f00272ebecf9c415ceec8197abb936432";
            }

            else
            {
                return null;
            }
        }
    }
}
