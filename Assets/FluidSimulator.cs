using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;
using UnityEngine.UIElements;

public class FluidSimulator : MonoBehaviour
{
    int size;
    float[,] densities;
    float[,] velX;
    float[,] velY;
    float[,] velX_prev;
    float[,] velY_prev;
    float[,] densities_prev;
    bool[,] boundaries;
    int[,] queue;
    float viscosity;
    float diffusion;
    bool begin_sim;
    int mouse_setting;
    int queue_setting;
    int back_setting;
    Texture2D texture;
    SpriteRenderer sr;
    Vector3 prev_mouse;

    // Start is called before the first frame update
    void Start()
    {
        reset_sim();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            begin_sim = !begin_sim;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            reset_sim();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            mouse_setting = (mouse_setting + 1) % 3;
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            queue_setting = 1;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            queue_setting = 2;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            queue_setting = 3;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            queue_setting = 4;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            back_setting = (back_setting + 1) % 2;
        }
        if (Time.frameCount == 0)
        {
            return;
        }
        float dt = Time.deltaTime;
        new_boundaries(size);
        new_queue(size);
        if (Input.GetKeyDown(KeyCode.E))
        {
            set_queue(size);
        }
        if (begin_sim)
        {
            set_mouse_velocities(size);
            vel_step(size, velX, velY, velX_prev, velY_prev, viscosity, dt);
            dens_step(size, densities, densities_prev, velX, velY, diffusion, dt);
        }
        set_background(size);
    }

    void reset_sim()
    {
        size = 50;
        densities = new float[size + 2, size + 2];
        velX = new float[size + 2, size + 2];
        velY = new float[size + 2, size + 2];
        velX_prev = new float[size + 2, size + 2];
        velY_prev = new float[size + 2, size + 2];
        densities_prev = new float[size + 2, size + 2];
        boundaries = new bool[size + 2, size + 2];
        queue = new int[size + 2, size + 2];
        viscosity = 0;
        diffusion = 0.0002f;
        begin_sim = false;
        texture = new Texture2D(size, size);
        Color black = new Color(0, 0, 0);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                texture.SetPixel(i, j, black);
            }
        }
        texture.Apply();
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        prev_mouse = new Vector3(0, 0, 0);
        mouse_setting = 0;
        queue_setting = 1;
        back_setting = 0;
    }

    void new_queue(int N)
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (pos.x >= -sr.bounds.extents.x && pos.x <= sr.bounds.extents.x &&
                pos.y >= -sr.bounds.extents.y && pos.y <= sr.bounds.extents.y)
            {
                int x = (int)(N * (pos.x + sr.bounds.extents.x) / sr.bounds.size.x);
                int y = (int)(N * (pos.y + sr.bounds.extents.y) / sr.bounds.size.y);
                if (x > 0 && x < N - 1 && y > 0 && y < N - 1)
                {
                    queue[x, y] = queue_setting;
                }
            }
        }
    }

    void set_queue(int N)
    {
        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                if (queue[i, j] > 0 && !boundaries[i, j])
                {
                    add_density(i, j, 45f);
                    if (queue[i, j] == 1)
                    {
                        add_velocity(i, j, 0, 40f);
                    }
                    else if (queue[i, j] == 2)
                    {
                        add_velocity(i, j, -40f, 0);
                    }
                    else if (queue[i, j] == 3)
                    {
                        add_velocity(i, j, 0, -40f);
                    } else
                    {
                        add_velocity(i, j, 40f, 0);
                    }
                }
            }
        }
        queue = new int[size + 2, size + 2];
    }

    void new_boundaries(int N)
    {
        if (Input.GetKey(KeyCode.Mouse0))
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (pos.x >= -sr.bounds.extents.x && pos.x <= sr.bounds.extents.x && 
                pos.y >= -sr.bounds.extents.y && pos.y <= sr.bounds.extents.y)
            {
                int x = (int)(N * (pos.x + sr.bounds.extents.x) / sr.bounds.size.x);
                int y = (int)(N * (pos.y + sr.bounds.extents.y) / sr.bounds.size.y);
                if (x > 0 && x < N - 1 && y > 0 && y < N - 1)
                {
                    add_boundaries(x, y, x + 1, y + 1);
                }
            }
        }
    }

    void set_mouse_velocities(int N)
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mouse_setting == 0)
        {
            add_density(N / 2, N / 2, 0.5f);
            add_velocity(N / 2, N / 2, pos.x, pos.y);
        } else if (mouse_setting == 1)
        {
            if (pos.x >= -sr.bounds.extents.x && pos.x <= sr.bounds.extents.x &&
                pos.y >= -sr.bounds.extents.y && pos.y <= sr.bounds.extents.y)
            {
                int x = (int)(N * (pos.x + sr.bounds.extents.x) / sr.bounds.size.x);
                int y = (int)(N * (pos.y + sr.bounds.extents.y) / sr.bounds.size.y);
                if (x > 0 && x < N - 1 && y > 0 && y < N - 1)
                {
                    add_density(x, y, 0.5f);
                    add_velocity(x, y, pos.x - prev_mouse.x, pos.y - prev_mouse.y);
                    prev_mouse = pos;
                }
            }
        }
    }

    void set_background(int N)
    {
        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                if (boundaries[i, j])
                {
                    Color black = new Color(0, 0, 0);
                    texture.SetPixel(i - 1, j - 1, black);
                } else if (queue[i, j] > 0)
                {
                    Color blue = new Color(0, 0, 255);
                    texture.SetPixel(i - 1, j - 1, blue);
                } else
                {
                    Color c;
                    if (back_setting == 0)
                    {
                        c = new Color(255, 255, 255, densities[i, j]);
                    }
                    else
                    {
                        c = Color.HSVToRGB(densities[i, j] * 2, 255, 255);
                    }
                    texture.SetPixel(i - 1, j - 1, c);
                }
            }
        }
        texture.Apply();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    void add_density(int x, int y, float dens)
    {
        densities[x, y] += dens;
    }
    
    void add_velocity(int x, int y, float addX, float addY)
    {
        velX[x, y] += addX;
        velY[x, y] += addY;
    }

    void add_boundaries(int x0, int y0, int x1, int y1)
    {
        for (int i = x0; i <= x1; i++)
        {
            for (int j = y0; j <= y1; j++)
            {
                boundaries[i, j] = true;
            }
        }
    }

    void add_source(int N, float[,] x, float[,] s, float dt)
    {
        for (int i = 0; i < N + 2; i++)
        {
            for (int j = 0; j < N + 2; j++)
            {
                x[i, j] += dt * s[i, j];
            }
        }
    }

    void diffuse(int N, int b, float[,] x, float[,] x0, float diff, float dt)
    {
        float a = dt * diff * N * N;
        for (int k = 0; k < 20; k++)
        {
            for (int i = 1; i <= N; i++)
            {
                for (int j = 1; j <= N; j++)
                {
                    x[i, j] = (x0[i, j] + a * (x[i - 1, j] + x[i + 1, j] + x[i, j - 1] + x[i, j + 1])) / (1 + 4 * a);
                }
            }
            set_bnd(N, b, x);
        }
    }

    void advect(int N, int b, float[,] d, float[,] d0, float[,] u, float[,] v, float dt)
    {
        float dt0 = dt * N;
        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                float x = i - dt0 * u[i, j];
                float y = j - dt0 * v[i, j];
                if (x < 0.5)
                {
                    x = 0.5f;
                } else if (x > (float)N + 0.5f)
                {
                    x = (float) N + 0.5f;
                }
                int i0 = (int) x;
                int i1 = i0 + 1;
                if (y < 0.5)
                {
                    y = 0.5f;
                } else if (y > (float)N + 0.5f)
                {
                    y = (float)N + 0.5f;
                }
                int j0 = (int) y;
                int j1 = j0 + 1;
                float s1 = x - i0;
                float s0 = 1 - s1;
                float t1 = y - j0;
                float t0 = 1 - t1;
                d[i, j] = s0 * (t0 * d0[i0, j0] + t1 * d0[i0, j1]) + s1 * (t0 * d0[i1, j0] + t1 * d0[i1, j1]);
            }
        }
        set_bnd(N, b, d);
    }

    void dens_step(int N, float[,] x, float[,] x0, float[,] u, float[,] v, float diff, float dt)
    {
        float[,] temp = x0;
        x0 = x;
        x = temp;
        diffuse(N, 0, x, x0, diff, dt);
        temp = x0;
        x0 = x;
        x = temp;
        advect(N, 0, x, x0, u, v, dt);
    }

    void vel_step(int N, float[,] u, float[,] v, float[,] u0, float[,] v0, float visc, float dt)
    {
        float[,] temp = u0;
        u0 = u;
        u = temp;
        diffuse(N, 1, u, u0, visc, dt);
        temp = v0;
        v0 = v;
        v = temp;
        diffuse(N, 2, v, v0, visc, dt);
        project(N, u, v, u0, v0);
        temp = u0;
        u0 = u;
        u = temp;
        temp = v0;
        v0 = v;
        v = temp;
        advect(N, 1, u, u0, u0, v0, dt);
        advect(N, 2, v, v0, u0, v0, dt);
        project(N, u, v, u0, v0);
    }

    void project(int N, float[,] u, float[,] v, float[,] p, float[,] div)
    {
        float h = 1 / (float)N;
        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                div[i, j] = -0.5f * h * (u[i + 1, j] - u[i - 1, j] + v[i, j + 1] - v[i, j - 1]);
                p[i, j] = 0;
            }
        }
        set_bnd(N, 0, div);
        set_bnd(N, 0, p);
        for (int k = 0; k < 20; k++)
        {
            for (int i = 1; i <= N; i++)
            {
                for (int j = 1; j <= N; j++)
                {
                    p[i, j] = (div[i, j] + p[i - 1, j] + p[i + 1, j] + p[i, j - 1] + p[i, j + 1]) / 4;
                }
            }
            set_bnd(N, 0, p);
        }
        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                u[i, j] -= 0.5f * (p[i + 1, j] - p[i - 1, j]) / h;
                v[i, j] -= 0.5f * (p[i, j + 1] - p[i, j - 1]) / h;
            }
        }
        set_bnd(N, 1, u);
        set_bnd(N, 2, v);
    }

    void set_bnd(int N, int b, float[,] x)
    {
        for (int i = 1; i <= N; i++)
        {
            if (b == 1)
            {
                x[0, i] = -x[1, i];
                x[N + 1, i] = -x[N, i];
            } else
            {
                x[0, i] = x[1, i];
                x[N + 1, i] = x[N, i];
            }
            if (b == 2)
            {
                x[i, 0] = -x[i, 1];
                x[i, N + 1] = -x[i, N];
            } else
            {
                x[i, 0] = x[i, 1];
                x[i, N + 1] = x[i, N];
            }
        }
        x[0, 0] = 0.5f * (x[1, 0] + x[0, 1]);
        x[0, N + 1] = 0.5f * (x[1, N + 1] + x[0, N]);
        x[N + 1, 0] = 0.5f * (x[N, 0] + x[N + 1, 1]);
        x[N + 1, N + 1] = 0.5f * (x[N, N + 1] + x[N + 1, N]);

        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                if (!boundaries[i, j])
                {
                    continue;
                }
                if (b == 1)
                {
                    if (!boundaries[i - 1, j])
                    {
                        x[i, j] = -x[i - 1, j];
                    }
                    if (!boundaries[i + 1, j])
                    {
                        x[i, j] = -x[i + 1, j];
                    }
                }
                else if (b == 2)
                {
                    if (!boundaries[i, j - 1])
                    {
                        x[i, j] = -x[i, j - 1];
                    }
                    if (!boundaries[i, j + 1])
                    {
                        x[i, j] = -x[i, j + 1];
                    }
                }
                else
                {
                    float count = 0;
                    float total = 0;
                    if (!boundaries[i - 1, j])
                    {
                        total += x[i - 1, j];
                        count++;
                    }
                    if (!boundaries[i + 1, j])
                    {
                        total += x[i + 1, j];
                        count++;
                    }
                    if (!boundaries[i, j - 1])
                    {
                        total += x[i, j - 1];
                        count++;
                    }
                    if (!boundaries[i, j + 1])
                    {
                        total += x[i, j + 1];
                        count++;
                    }
                    if (count > 0)
                    {
                        x[i, j] = total / count;
                    }
                    else
                    {
                        x[i, j] = 0;
                    }
                }
            }
        }
    }
}
