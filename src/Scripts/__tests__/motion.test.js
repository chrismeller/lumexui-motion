import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('motion', () => ({ animate: vi.fn(() => Promise.resolve()) }));

const { animate } = await import('motion');

// A fresh copy of the module for every test, so the layout registry starts empty.
async function loadModule() {
    vi.resetModules();
    return (await import('../motion.js')).motion;
}

function element(rect) {
    const el = document.createElement('div');
    el.getBoundingClientRect = () => ({ x: 0, y: 0, width: 100, height: 10, ...rect });
    document.body.appendChild(el);
    return el;
}

describe('motion interop', () => {
    let motion;
    let consoleError;

    beforeEach(async () => {
        animate.mockClear();
        consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
        motion = await loadModule();
    });

    afterEach(() => {
        consoleError.mockRestore();
        document.body.innerHTML = '';
    });

    describe('animateEnter and animateExit', () => {
        it('animates the element with the enter props and the transition', async () => {
            const el = element();
            const transition = { duration: 0.2 };

            await motion.animateEnter(el, { enter: { opacity: 1 }, exit: { opacity: 0 }, transition });

            expect(animate).toHaveBeenCalledExactlyOnceWith(el, { opacity: 1 }, transition);
        });

        it('animates the element with the exit props and the transition', async () => {
            const el = element();
            const transition = { duration: 0.2 };

            await motion.animateExit(el, { enter: { opacity: 1 }, exit: { opacity: 0 }, transition });

            expect(animate).toHaveBeenCalledExactlyOnceWith(el, { opacity: 0 }, transition);
        });

        it('logs instead of throwing when the element is not an element', async () => {
            await expect(motion.animateEnter(null, { enter: { opacity: 1 } })).resolves.toBeUndefined();

            expect(animate).not.toHaveBeenCalled();
            expect(consoleError).toHaveBeenCalledWith('animateEnter:', expect.any(Error));
        });
    });

    describe('animateLayoutId', () => {
        it('does not animate the first element it sees for a layoutId', async () => {
            await motion.animateLayoutId(element(), null, 'underline');

            expect(animate).not.toHaveBeenCalled();
        });

        it('animates a new element from where the previous one with the same layoutId was', async () => {
            const prev = element({ x: 0, width: 100 });
            const curr = element({ x: 200, width: 50 });

            await motion.animateLayoutId(prev, null, 'underline');
            prev.remove();
            await motion.animateLayoutId(curr, { transition: { duration: 0.3 } }, 'underline');

            // deltaX -200, deltaWidth +50 -> x = -200 + 25; scaleX = 100 / 50.
            expect(animate).toHaveBeenCalledExactlyOnceWith(
                curr,
                { x: [-175, 0], scaleX: [2, 1] },
                expect.objectContaining({ duration: 0.3, onUpdate: expect.any(Function) }));
        });

        it('animates from the last element seen, not the first', async () => {
            await motion.animateLayoutId(element({ x: 0 }), null, 'underline');
            await motion.animateLayoutId(element({ x: 300 }), null, 'underline');
            animate.mockClear();

            const curr = element({ x: 100 });
            await motion.animateLayoutId(curr, null, 'underline');

            expect(animate).toHaveBeenCalledExactlyOnceWith(curr, { x: [200, 0], scaleX: [1, 1] }, expect.anything());
        });

        it('does not animate when the same element reports the same layoutId again', async () => {
            const el = element();

            await motion.animateLayoutId(el, null, 'underline');
            await motion.animateLayoutId(el, null, 'underline');

            expect(animate).not.toHaveBeenCalled();
        });

        it('keeps different layoutIds apart', async () => {
            await motion.animateLayoutId(element(), null, 'a');
            await motion.animateLayoutId(element(), null, 'b');

            expect(animate).not.toHaveBeenCalled();
        });

        it('does not keep an element that has left the page alive', async () => {
            let el = element();
            const collected = new WeakRef(el);

            await motion.animateLayoutId(el, null, 'underline');
            el.remove();
            el = null;

            await forceGc();

            expect(collected.deref()).toBeUndefined();
        });

        it('still animates from a previous element that has been garbage collected', async () => {
            let prev = element({ x: 0, width: 100 });
            await motion.animateLayoutId(prev, null, 'underline');
            prev.remove();
            prev = null;
            await forceGc();

            const curr = element({ x: 50, width: 100 });
            await motion.animateLayoutId(curr, null, 'underline');

            expect(animate).toHaveBeenCalledExactlyOnceWith(curr, { x: [-50, 0], scaleX: [1, 1] }, expect.anything());
        });
    });
});

// Lets pending promise jobs and timers drop their references before collecting.
async function forceGc() {
    for (let i = 0; i < 3; i++) {
        await new Promise(resolve => setTimeout(resolve, 0));
        globalThis.gc();
    }
}
