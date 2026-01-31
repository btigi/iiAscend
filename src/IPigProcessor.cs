using ii.Ascend.Model;

namespace ii.Ascend;

public interface IPigProcessor
{
    bool CanHandle(string filename);
    List<(string filename, byte[] bytes)> Read(string filename);
    (List<ImageInfo> images, List<SoundInfo> sounds, List<(string filename, byte[] data)> pofFiles, D1PigGameData? gameData) ReadDetailed(string filename);
}
