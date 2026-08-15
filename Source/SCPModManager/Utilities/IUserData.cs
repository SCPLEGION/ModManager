using Verse;

namespace SCPModManager;

public interface IUserData : IExposable
{
    public string FilePath { get; }

    public void Write();
}