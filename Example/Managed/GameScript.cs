using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameProject;

public abstract class GameScript
{
    public abstract void OnAwake();
    public abstract void OnStart();
    public abstract void OnUpdate(float deltaTime);
}
