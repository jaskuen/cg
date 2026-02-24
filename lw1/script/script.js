const CANVAS_ID = 'canvas';
const ANTI_ALIAS_SWITCH_ID = 'anti-alias';

const LOCAL_STORAGE_TASK_NAME = 'currentTask';

const DRAW_INTERVAL = 50;

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

    plot(x, y, canUseAntiAlias = false) {
        if (x - this.pixelSize / 2 > this.width || x + this.pixelSize / 2 < 0) {
            return;
        }
        if (y - this.pixelSize / 2 > this.height || y + this.pixelSize / 2 < 0) {
            return;
        }

        const ps = this.pixelSize;
        const px = Math.round(x - ps / 2);
        const py = Math.round(y - ps / 2);
        this.ctx.fillRect(px, py, ps, ps);

        if (canUseAntiAlias && this.useAntiAliasing) {
            let delta = 7;
            switch (ps) {
                case 1:
                case 2: break;
                case 3:
                case 4 : delta = 8; break;
                default: delta = 10; break;
            }

            const count = 10;
            const alphaDelta = 0.4 / count;

            for (let i = 1; i <= 10; i++) {
                let curDelta = ps / (delta * i);
                this.ctx.globalAlpha = 0.5 + (i * alphaDelta);

                this.ctx.fillRect(px - curDelta, py - curDelta, ps + curDelta * 2, ps + curDelta * 2);
                this.ctx.globalAlpha = 1;
            }
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
        if (color !== null) this.ctx.borderColor = color;
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
        let d = 10 - 2 * y;

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

        if (radius < 1) return;

        let x = 0;
        let y = Math.round(radius);
        let d = 3 - 2 * y;

        let startPixelSize = this.pixelSize;

        // рисуем край
        this.circle(xc, yc, radius - startPixelSize, fillColor);

        this.setPixelSize(startPixelSize * 3);

        while (y >= x) {
            this.line(xc, yc, xc + x - startPixelSize, yc + y - startPixelSize, fillColor, true);
            this.line(xc, yc, xc - x + startPixelSize, yc + y - startPixelSize, fillColor, true);
            this.line(xc, yc, xc - x + startPixelSize, yc - y + startPixelSize, fillColor, true);
            this.line(xc, yc, xc + y - startPixelSize, yc + x - startPixelSize, fillColor, true);
            this.line(xc, yc, xc + y - startPixelSize, yc - x + startPixelSize, fillColor, true);
            this.line(xc, yc, xc - y + startPixelSize, yc + x - startPixelSize, fillColor, true);
            this.line(xc, yc, xc - y + startPixelSize, yc - x + startPixelSize, fillColor, true);
            this.line(xc, yc, xc + x - startPixelSize, yc - y + startPixelSize, fillColor, true);

            x++;

            if (d > 0) {
                y--;
                d += 4 * (x - y) + 10;
            } else {
                d += 4 * x + 6;
            }
        }

        this.setPixelSize(startPixelSize);

        this.circle(xc, yc, radius, strokeColor);
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
        // ставить начало координат в (x; y), потом обратно
        const x = 320;
        const y = 150 + deltaY;

        canvas.setColor('#FF0000');
        let prevSize = canvas.pixelSize;
        canvas.setPixelSize(30);
        canvas.line(x + 15, y + 15, x + 30, y + 35);
        canvas.line(x + 60, y + 35, x + 75, y + 15);
        canvas.setPixelSize(prevSize);
        canvas.filledRect(x, y, x + 30, y + 100);
        canvas.filledRect(x + 60, y, x + 90, y + 100);
    }
}

class Letter3 extends JumpingFiguresDrawing {
    onDraw(canvas, deltaY) {
        const x = 450;
        const y = 150 + deltaY;

        canvas.setColor('#654321');
        canvas.filledRect(x, y, x + 70, y + 25);
        canvas.filledRect(x, y + 40, x + 70, y + 65);
        canvas.filledRect(x, y + 75, x + 70, y + 100);
        canvas.filledRect(x, y, x + 30, y + 100);
    }
}

class Menu {
    addButton(id, onClick) {
        if (id == null) throw("Id cannot be null");

        const button = document.getElementById(id);
        if (button == null) throw("Failed to get ");

        button.addEventListener('click', onClick);
    }
}

class TaskRenderer {
    constructor() {
        this.taskMap = new Map();
        let currentTask = parseInt(localStorage.getItem(LOCAL_STORAGE_TASK_NAME));

        if (currentTask == null || currentTask === 0) {
            currentTask = 1;
        }

        this.currentTask = currentTask;

    }

    setCurrentTask(num, doRerender) {
        this.currentTask = num;
        localStorage.setItem(LOCAL_STORAGE_TASK_NAME, num);

        if (doRerender) {
            this.renderTask();
        }
    }

    addTask(taskNum, func) {
        this.taskMap.set(taskNum, func);
    }

    renderTask() {
        const func = this.taskMap.get(this.currentTask);
        func();
    }
}
function main() {
    const canvas = new MyCanvas(window.document, CANVAS_ID, {pixelSize: 2});
    const taskRenderer = new TaskRenderer(canvas);

    initMenu(canvas, taskRenderer);

    taskRenderer.renderTask();
}

function initMenu(canvas, taskRenderer) {
    taskRenderer.addTask(1, async () => { await task1(canvas, taskRenderer); });
    taskRenderer.addTask(2, () => { task2(canvas); });
    taskRenderer.addTask(3, () => { task3(canvas); });

    const menu = new Menu();

    menu.addButton('task1', async () => {
        if (taskRenderer.currentTask !== 1) {
            taskRenderer.setCurrentTask(1, true);
        }
    });

    menu.addButton('task2', async () => {
        if (taskRenderer.currentTask !== 2) {
            taskRenderer.setCurrentTask(2, true);
        }
    });

    menu.addButton('task3', async () => {
        if (taskRenderer.currentTask !== 3) {
            taskRenderer.setCurrentTask(3, true);
        }
    });

    let antiAliasSwitch = document.getElementById(ANTI_ALIAS_SWITCH_ID);
    antiAliasSwitch.addEventListener('click', async () => {
        canvas.setAntiAliasing(antiAliasSwitch.checked);
        if (taskRenderer.currentTask !== 1) {
            await taskRenderer.renderTask();
        }
    });
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function drawAnimation(func, taskRenderer, taskNumber, delay) {
    while (taskRenderer.currentTask === taskNumber) {
        func();
        await sleep(delay);
    }
}

async function task1(canvas, taskRenderer) {
    const letter1 = new Letter1();
    const letter2 = new Letter2();
    const letter3 = new Letter3();

    const shapes = [letter1, letter2, letter3];

    await drawAnimation(() => {
        renderShapes(canvas, shapes)
    }, taskRenderer, 1, DRAW_INTERVAL);
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
    const wheelRadius = 32;
    const wheelY      = cy + 50;

    canvas.clear();
    canvas.setPixelSize(2);

    // Кузов
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

    // крыша
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
    canvas.circle(cx - 110, wheelY, wheelRadius - 8, wheelRimColor);
    canvas.filledCircle(cx + 110, wheelY, wheelRadius, wheelColor, wheelColor);
    canvas.circle(cx + 110, wheelY, wheelRadius - 8, wheelRimColor);

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

    canvas.setPixelSize(1);
    canvas.circle(100, 100, 50);

    canvas.filledCircle(200, 200, 50, '#000000', '#123456');

    canvas.setPixelSize(3);
    canvas.circle(300, 100, 50);

    canvas.filledCircle(400, 200, 50, '#000000', '#123456');
}

function renderShapes(canvas, figures) {
    canvas.clear();
    figures.forEach(figure => figure.draw(canvas));
}

document.addEventListener('DOMContentLoaded', main )