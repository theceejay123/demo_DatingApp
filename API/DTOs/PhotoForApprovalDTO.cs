namespace API.DTOs
{
  public class PhotoForApprovalDTO
  {
    public int Id { get; set; }
    public string Url { get; set; }
    public string Username { get; set; }
    public bool isApproved { get; set; }
  }
}