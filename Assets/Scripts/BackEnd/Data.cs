namespace Vertex.Backend.Data
{
    [System.Serializable]
    public class EncryptedScore
    {
        public int score;
        public string userId;
        public string salt; // This will hold the createdAt timestamp string
    }
}