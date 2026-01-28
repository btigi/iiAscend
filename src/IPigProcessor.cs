using ii.Ascend.Model;

namespace ii.Ascend;

public interface IPigProcessor
{
    bool CanHandle(string filename);
    List<(string filename, byte[] bytes)> Read(string filename);
    (List<ImageInfo> images, List<SoundInfo> sounds, D1PigGameData? gameData) ReadDetailed(string filename);
}
