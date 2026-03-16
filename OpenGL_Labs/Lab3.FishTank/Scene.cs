using System.Drawing;
using System.Numerics;
using Lab3.FishTank.Objects;
using Lab3.FishTank.Shapes;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab3.FishTank;

public class Scene : UpdateableObject
{
    private const int FishesCount = 7;
    private List<Fish> _fishes;

    private readonly List<Color> _fishColors =
    [
        Color.OrangeRed,
        Color.LawnGreen,
        Color.Purple,
        Color.BlueViolet,
    ];

    private readonly World _world;

    private Random _random;

    public Scene(World world)
    {
        _world = world;
        _fishes = [];
        _random = new Random();

        SpawnRocks();
        SpawnAlgae(10);

        for (int i = 0; i < FishesCount; i++)
        {
            float velocity = (0.5f - (float)_random.NextDouble()) / 50f;
            Fish fish = new Fish(new Vector3(-1f + 0.5f * i, 1f - 0.3f * i, 0), RandomColor(), i % 3, velocity);

            _world.AddObject(fish);
            foreach (RenderableObject obj in fish.Figures)
            {
                _world.AddObject(obj);
            }
            _fishes.Add(fish);
        }
    }

    private void SpawnAlgae(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var algae = new Algae(_random);
            float height = 0.4f + (float)_random.NextDouble() * 0.8f;
            algae.SetScale(new Vector3(0.1f, height, 1f));
            algae.SetPosition(new Vector3(-2.8f + (float)_random.NextDouble() * 5.6f, -1.3f + height/2, 0));
        
            _world.AddObject(algae);
        }
    }

    public override void OnUpdate(double dt)
    {
    }
    
    private void SpawnRocks()
    {
        int rockCount = _random.Next(3, 6);
        for (int i = 0; i < rockCount; i++)
        {
            Ellipse rock = new Ellipse(8); 
            rock.FillColor = Color.Gray;
            rock.OutlineColor = Color.DimGray;
            rock.HasOutline = true;
        
            float scaleX = 0.2f + (float)_random.NextDouble() * 0.3f;
            float scaleY = 0.1f + (float)_random.NextDouble() * 0.2f;
            rock.SetScale(new Vector3(scaleX, scaleY, 1f));
            rock.SetPosition(new Vector3(-2.5f + (float)_random.NextDouble() * 5f, -1.2f, 0));
        
            _world.AddObject(rock);
        }
    }

    private Color RandomColor()
    {
        int rnd = _random.Next(_fishColors.Count);

        return _fishColors[rnd];
    }
}