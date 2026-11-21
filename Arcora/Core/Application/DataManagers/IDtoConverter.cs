using Arcora.Shared.Base;
using UIX.ViewController.Engine.DataBaseObjects;

namespace Arcora.Core.Application.DataManagers
{
    public interface IDtoConverter<TTableObject, TDto> where TTableObject : UIXTableObjectBase where TDto : DtoBase
    {
        TDto ToDto(TTableObject obj);

        void ApplyDto(TDto dto, TTableObject obj);
    }
}
