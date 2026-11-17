namespace Rebuild.Saving
{



    public interface IDataPersistence
    {

        void LoadData(GameDataDTO data);


        void SaveData(GameDataDTO data);
    }
}
