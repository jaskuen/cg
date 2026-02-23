const CANVAS_ID = 'canvas';
const ANTI_ALIAS_SWITCH_ID = 'anti-alias';

const LOCAL_STORAGE_TASK_NAME = 'currentTask';

const DRAW_INTERVAL = 20;

class MyCanvas {
    constructor(document, canvasElementId, options = {}) {
        let canvasElement = document.getElementById(canvasElementId);

        if (!canvasElementId || !(canvasElement instanceof HTMLCanvasElement)) {
            throw new Error("Failed to get canvas from ID");
        }

        this.canvas = canvasElement;
        this.ctx = this.canvas.getContext('2d');

        this.pixelSize    = options.pixelSize   ?? 2;
        this.defaultColor = options.color       ?? '#000000';
        this.background   = options.background  ?? '#ffffff';

        this.currentColor = this.defaultColor;
        this.clear();
    }

    setColor(color) {
        this.currentColor = color;
        this.ctx.fillStyle = color;
    }

    setPixelSize(size) {
        this.pixelSize = Math.max(1, Math.round(size));
    }

    setBackground(color) {
        this.background = color;
    }

    plot(x, y, canUseAntiAlias = false) {
        if (x + this.pixelSize > this.width || x - this.pixelSize / 2 < 0) {
            return;
        }
        if (y + this.pixelSize > this.height || y - this.pixelSize / 2 < 0) {
            return;
        }

        const ps = this.pixelSize;
        const px = Math.round(x - ps / 2);
        const py = Math.round(y - ps / 2);
        this.ctx.fillRect(px, py, ps, ps);

        if (canUseAntiAlias && this.useAntiAliasing) {
            this.ctx.globalAlpha = 0.8;
            let delta = 7;
            switch (ps) {
                case 1:
                case 2: break;
                case 3:
                case 4 : delta = 8; break;
                default: delta = 10; break;
            }

            delta = ps / delta;

            this.ctx.fillRect(px - delta, py - delta, ps + delta * 2, ps + delta * 2);
            this.ctx.globalAlpha = 1;
        }
    }

    clear() {
        this.ctx.fillStyle = this.background;
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        this.ctx.fillStyle = this.currentColor; // restore current drawing color
    }

    line(x0, y0, x1, y1, color = null, useAntiAliasing = false) {
        if (color !== null) this.setColor(color);

        let dx = Math.abs(x1 - x0);
        let dy = Math.abs(y1 - y0);
        let sx = x0 < x1 ? 1 : -1;
        let sy = y0 < y1 ? 1 : -1;
        let err = dx - dy;

        while (true) {
            this.plot(x0, y0, useAntiAliasing);

            if (x0 === x1 && y0 === y1) break;

            let e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }

    rect(x0, y0, x1, y1, color = null) {
        this.ctx.borderColor = color;
        this.ctx.lineWidth = this.pixelSize;
        this.ctx.strokeRect(x0, y0, x1 - x0, y1 - y0);
    }

    filledRect(x0, y0, x1, y1, strokeColor = null, fillColor = null) {
        if (strokeColor !== null) {
            this.ctx.borderColor = strokeColor;
            this.ctx.lineWidth = this.pixelSize;
            this.ctx.strokeRect(x0, y0, x1 - x0, y1 - y0);
        }
        if (fillColor !== null) this.ctx.fillStyle = fillColor;

        this.ctx.fillRect(x0, y0, x1 - x0, y1 - y0);
    }

    circle(xc, yc, radius, color = null) {
        if (color !== null) this.setColor(color);
        if (radius < 1) return;

        let x = 0;
        let y = Math.round(radius);
        let d = 3 - 2 * y;

        while (y >= x) {
            this.plot(xc + x, yc + y, true);
            this.plot(xc - x, yc + y, true);
            this.plot(xc - x, yc - y, true);
            this.plot(xc + y, yc + x, true);
            this.plot(xc + y, yc - x, true);
            this.plot(xc - y, yc + x, true);
            this.plot(xc - y, yc - x, true);
            this.plot(xc + x, yc - y, true);

            x++;

            if (d > 0) {
                y--;
                d += 4 * (x - y) + 10;
            } else {
                d += 4 * x + 6;
            }
        }
    }

    filledCircle(xc, yc, radius, strokeColor = null, fillColor = null) {
        if (fillColor !== null) this.setColor(fillColor);
        for (let r = 0; r <= radius; r++) {
            if (r === radius) {
                if (strokeColor !== null) this.setColor(strokeColor);
            }
            this.circle(xc, yc, r);
        }
    }

    setAntiAliasing(useAntiAliasing) {
        this.useAntiAliasing = useAntiAliasing;
    }

    get width()  { return this.canvas.width;  }
    get height() { return this.canvas.height; }
}

class JumpingFiguresDrawing {
    constructor() {
        this.down = Math.round(Math.random() * 100);
        this.currentPos = 0;
        this.direction = 1;

        this.jumpRange = 100;
        this.delta = 2;

        this.speed = Math.round(Math.random() * 20);
    }

    draw(canvas) {
        let dy = this.currentPos;

        // равноускоренное движение
        this.currentPos += this.speed * this.direction;
        this.speed += this.delta * this.direction;   // здесь + вместо -, т.к. delta теперь ускорение

        // отскок от нижней границы (down)
        if (this.currentPos >= this.down) {
            this.currentPos = this.down;
            this.direction = -1;
            this.speed = Math.abs(this.speed) - 2;   // скорость становится положительной, направление вверх
        }

        this.onDraw(canvas, dy);
    }

    onDraw(canvas, deltaY) {}
}

class Letter1 extends JumpingFiguresDrawing {
    onDraw(canvas, deltaY) {
        const x = 150;
        const y = 150 + deltaY;

        canvas.setColor('#123456');
        canvas.filledRect(x, y, x + 30, y + 100);
        canvas.filledRect(x + 45, y, x + 75, y + 100);
        canvas.filledRect(x + 90, y, x + 120, y + 100);
        canvas.filledRect(x, y + 70, x + 120, y + 100);
    }
}

class Letter2 extends JumpingFiguresDrawing {
    onDraw(canvas, deltaY) {
        const x = 320;
        const y = 150 + deltaY;

        canvas.setColor('#654321');
        canvas.filledRect(x, y, x + 70, y + 25);
        canvas.filledRect(x, y + 40, x + 70, y + 65);
        canvas.filledRect(x, y + 75, x + 70, y + 100);
        canvas.filledRect(x, y, x + 30, y + 100);
    }
}

class Letter3 extends JumpingFiguresDrawing {
    onDraw(canvas, deltaY) {
        const x = 450;
        const y = 150 + deltaY;

        canvas.setColor('#FF0000');
        canvas.filledRect(x, y, x + 70, y + 25);
        canvas.filledCircle(x + 15, y + 85, 13);
        canvas.filledRect(x, y, x + 30, y + 85);
        canvas.filledRect(x - 30, y + 70, x + 15, y + 100);
        canvas.filledRect(x + 60, y, x + 90, y + 100);
    }
}

let currentTask = 1;

function main() {
    const canvas = new MyCanvas(window.document, CANVAS_ID, {pixelSize: 2});
    initMenu(canvas);

    renderCurrentTask(canvas);
}

async function renderCurrentTask(canvas) {
    switch (currentTask) {
        case 1: await task1(canvas); break;
        case 2: task2(canvas); break;
        case 3: task3(canvas); break;
    }
}

function initMenu(canvas) {
    currentTask = parseInt(localStorage.getItem(LOCAL_STORAGE_TASK_NAME));

    if (currentTask == null || currentTask === 0) {
        currentTask = 1;
    }

    const task1 = document.getElementById('task1');
    const task2 = document.getElementById('task2');
    const task3 = document.getElementById('task3');

    task1.addEventListener('click', async (event) => {
        if (currentTask !== 1) {
            currentTask = 1;
            localStorage.setItem(LOCAL_STORAGE_TASK_NAME, '1');
            await renderCurrentTask(canvas);
        }
    });
    task2.addEventListener('click', async (event) => {
        if (currentTask !== 2) {
            currentTask = 2;
            localStorage.setItem(LOCAL_STORAGE_TASK_NAME, '2');
            await renderCurrentTask(canvas);
        }
    });
    task3.addEventListener('click', async (event) => {
        if (currentTask !== 3) {
            currentTask = 3;
            localStorage.setItem(LOCAL_STORAGE_TASK_NAME, '3');
            await renderCurrentTask(canvas);
        }
    });

    let antiAliasSwitch = document.getElementById(ANTI_ALIAS_SWITCH_ID);
    antiAliasSwitch.addEventListener('click', async (event) => {
        canvas.setAntiAliasing(antiAliasSwitch.checked);
        if (currentTask !== 1) {
            await renderCurrentTask(canvas);
        }
    })
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function task1(canvas) {
    const letter1 = new Letter1();
    const letter2 = new Letter2();
    const letter3 = new Letter3();

    const shapes = [letter1, letter2, letter3];

    while (currentTask === 1) {
        renderShapes(canvas, shapes);
        await sleep(DRAW_INTERVAL);
    }
}

function task2(canvas) {
    const basePixelSize = canvas.pixelSize;

    const bodyColor     = '#2c7bb6';
    const windowColor   = '#a5d8ff';
    const roofColor     = '#1f618d';
    const wheelColor    = '#333333';
    const wheelRimColor = '#dddddd';
    const poleColor     = '#555555';
    const black         = '#000000';
    const white         = '#ffffff';

    const cx = 400;
    const cy = 300;

    const bodyWidth   = 320;
    const bodyHeight  = 100;
    const wheelRadius = 32;
    const wheelY      = cy + 50;

    canvas.clear();

    // ─── Кузов (основной прямоугольник + скосы) ────────────────────────
    canvas.setColor(bodyColor);

    // основной корпус
    canvas.filledRect(
        cx - bodyWidth/2, cy - 60,
        cx + bodyWidth/2, cy + 40,
        black, bodyColor
    );

    // передняя часть (кабина, скос вперёд)
    canvas.filledRect(
        cx + 120, cy - 60,
        cx + 160, cy - 10,
        black, bodyColor
    );

    // небольшой скос сзади
    canvas.filledRect(
        cx - 170, cy - 45,
        cx - 160, cy + 20,
        black, bodyColor
    );

    // крыша (тёмная полоса сверху)
    canvas.filledRect(
        cx - bodyWidth/2 - 5, cy - 70,
        cx + bodyWidth/2 + 5, cy - 55,
        null, roofColor
    );

    // Полоска по центру
    canvas.setColor(white);
    canvas.line(
        cx - 140, cy + 5,
        cx + 140, cy + 5
    );

    // окна и дверь
    canvas.setColor(windowColor);
    for (let i = -3; i <= 2; i++) {
        let wx = cx + i * 45 - 22;

        if (i === 0) {
            canvas.filledRect(
                wx, cy - 45,
                wx + 42, cy + 35,
                black, roofColor
            );
            canvas.line(
                wx + 21, cy - 45,
                wx + 21, cy + 35,
                black
            );
            continue;
        }

        canvas.filledRect(
            wx, cy - 45,
            wx + 42, cy - 15,
            black, windowColor
        );
        canvas.line(
            wx + 21, cy - 45,
            wx + 21, cy - 15,
            black
        );
        canvas.line(
            wx, cy - 30,
            wx + 42, cy - 30,
            black
        );
    }

    // окно водителя
    canvas.filledRect(
        cx + 125, cy - 48,
        cx + 155, cy - 12,
        black, windowColor
    );

    // Колеса
    canvas.filledCircle(cx - 110, wheelY, wheelRadius, wheelColor, wheelColor);
    canvas.circle     (cx - 110, wheelY, wheelRadius - 8, wheelRimColor);
    canvas.filledCircle(cx + 110, wheelY, wheelRadius, wheelColor, wheelColor);
    canvas.circle     (cx + 110, wheelY, wheelRadius - 8, wheelRimColor);

    // Провода для усов
    canvas.setColor('#000000');
    canvas.line(0, cy - 130, canvas.width, cy - 130);

    // Усы
    canvas.setColor(poleColor);
    canvas.setPixelSize(canvas.pixelSize * 1.5);

    canvas.line(
        cx - 50, cy - 68,
        cx - 130, cy - 130, null, true
    );

    canvas.line(
        cx + 50, cy - 68,
        cx + 130, cy - 130, null, true
    );

    canvas.setPixelSize(basePixelSize);
    canvas.filledCircle(cx - 130, cy - 130, 8, poleColor, poleColor);
    canvas.filledCircle(cx + 130, cy - 130, 8, poleColor, poleColor);

    canvas.setPixelSize(basePixelSize);
}

function task3(canvas) {
    canvas.clear();
}

function renderShapes(canvas, figures) {
    canvas.clear();
    figures.forEach(figure => figure.draw(canvas));
}

document.addEventListener('DOMContentLoaded', main )