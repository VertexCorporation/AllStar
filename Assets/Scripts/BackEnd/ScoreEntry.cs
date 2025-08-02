/***************************************************************************
 *  ScoreEntry (2025-06-08 - Refactored)
 *  -----------------------------------------------------------------------
 *  • Represents a score entry in both /highscores and /seasons/.../scoreboard.
 *  • NEW: Includes 'userId' for direct identification.
 *  • 'username' is now denormalized (copied) into the score document
 *    for high-performance reads, avoiding extra lookups.
 ***************************************************************************/

using Firebase.Firestore;

namespace Vertex.Backend
{
    [FirestoreData]
    public class ScoreEntry
    {
        [FirestoreProperty] public string userId { get; set; }
        [FirestoreProperty] public int score { get; set; }
        [FirestoreProperty] public long date { get; set; }
        public int rank;
        public string username;

        public bool IsMine()
        {
            if (!AuthService.IsSignedIn) return false;
            return userId == AuthService.UserId;
        }
    }
}