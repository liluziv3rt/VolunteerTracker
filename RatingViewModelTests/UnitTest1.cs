using Xunit;

namespace VolunteerTracker.Tests
{
    public class RatingViewModelTests
    {
        [Theory]
        [InlineData(1, "🥇")]
        [InlineData(2, "🥈")]
        [InlineData(3, "🥉")]
        [InlineData(4, "")]
        [InlineData(10, "")]
        public void GetMedal_ShouldReturnCorrectMedal(int rank, string expected)
        {
            var medal = GetMedal(rank);
            Assert.Equal(expected, medal);
        }

        [Theory]
        [InlineData(1, "#FFD700")]
        [InlineData(2, "#C0C0C0")]
        [InlineData(3, "#CD7F32")]
        [InlineData(4, "#5F6368")]
        [InlineData(10, "#5F6368")]
        public void GetRankColor_ShouldReturnCorrectColor(int rank, string expected)
        {
            var color = GetRankColor(rank);
            Assert.Equal(expected, color);
        }

        [Theory]
        [InlineData(1, "#FFF9E6")]
        [InlineData(2, "#F0F7FF")]
        [InlineData(3, "#FFF4EB")]
        [InlineData(4, "Transparent")]
        [InlineData(10, "Transparent")]
        public void GetRowBackground_ShouldReturnCorrectBackground(int rank, string expected)
        {
            var bg = GetRowBackground(rank);
            Assert.Equal(expected, bg);
        }

        [Theory]
        [InlineData("Иван", "Иванов", "ИИ")]
        [InlineData("Анна", "Петрова", "АП")]
        [InlineData("Сергей", null, "С")]
        [InlineData(null, "Кузнецов", "К")]
        [InlineData("", "", "")]
        [InlineData(null, null, "??")]
        public void GetInitials_ShouldReturnCorrectInitials(string firstName, string lastName, string expected)
        {
            var initials = GetInitials(firstName, lastName);
            Assert.Equal(expected, initials);
        }

        [Fact]
        public void GetAchievementProgress_ShouldReturnCorrectPercent()
        {
            int currentPoints = 120;
            int prevThreshold = 50;
            int nextThreshold = 200;

            double percent = CalculateProgress(currentPoints, prevThreshold, nextThreshold);

            Assert.Equal(46.67, percent, 2);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("rejected", true)]
        [InlineData("pending", false)]
        [InlineData("approved", false)]
        public void CanSendRequest_ShouldReturnCorrectValue(string requestStatus, bool expected)
        {
            var canSend = CanSendRequest(requestStatus);
            Assert.Equal(expected, canSend);
        }

        private static string GetMedal(int rank)
        {
            if (rank == 1) return "🥇";
            if (rank == 2) return "🥈";
            if (rank == 3) return "🥉";
            return "";
        }

        private static string GetRankColor(int rank)
        {
            if (rank == 1) return "#FFD700";
            if (rank == 2) return "#C0C0C0";
            if (rank == 3) return "#CD7F32";
            return "#5F6368";
        }

        private static string GetRowBackground(int rank)
        {
            if (rank == 1) return "#FFF9E6";
            if (rank == 2) return "#F0F7FF";
            if (rank == 3) return "#FFF4EB";
            return "Transparent";
        }

        private static string GetInitials(string firstName, string lastName)
        {
            if (firstName == null && lastName == null)
                return "??";
            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                return "";
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                return $"{firstName[0]}{lastName[0]}".ToUpper();
            if (!string.IsNullOrEmpty(firstName))
                return firstName[0].ToString().ToUpper();
            if (!string.IsNullOrEmpty(lastName))
                return lastName[0].ToString().ToUpper();
            return "??";
        }

        private static double CalculateProgress(int current, int prev, int next)
        {
            if (next <= prev) return 100;
            double total = next - prev;
            double earned = current - prev;
            if (earned <= 0) return 0;
            if (earned >= total) return 100;
            return (earned / total) * 100;
        }

        private static bool CanSendRequest(string requestStatus)
        {
            return string.IsNullOrEmpty(requestStatus) || requestStatus == "rejected";
        }
    }
}