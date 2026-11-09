using GameTimeNext.Shared.Base;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public interface IDtoConverter<TTableObject, TDto> where TTableObject : UIXTableObjectBase where TDto : DtoBase
    {
        TDto ToDto(TTableObject obj);

        void ApplyDto(TDto dto, TTableObject obj);
    }
}
