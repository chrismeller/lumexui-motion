import { animate as motionAnimate } from 'motion'
import { mergeDeep } from './utils.js'

// The last element seen for each layoutId and where it was. A new element with the same layoutId animates from that
// rect. The element is held through a WeakRef: it is only used to tell whether the same element reported again, and a
// strong reference would keep every element that ever had a layoutId alive after it left the page. The rect is a
// plain DOMRect, which doesn't hold on to the element.
const layoutRegistry = new Map();

async function animateEnter(ref, props) {
    try {
        await animateCore(ref, props, "enter");
    } catch (error) {
        console.error("animateEnter:", error);
    }
}

async function animateExit(ref, props) {
    try {
        await animateCore(ref, props, "exit");
    } catch (error) {
        console.error("animateExit:", error);
    }
}

async function animateLayoutId(ref, props, layoutId) {
    try {
        if (!(ref instanceof HTMLElement)) {
            throw new Error("Invalid element provided");
        }

        const curr = { ref: new WeakRef(ref), rect: ref.getBoundingClientRect() };
        const prev = layoutRegistry.get(layoutId);

        // Update the stored rect, whether or not this element animates.
        layoutRegistry.set(layoutId, curr);

        // If we already have a stored rect for this layoutId from another element,
        // animate from the prev -> curr.
        if (prev && prev.ref.deref() !== ref) {
            const deltaX = prev.rect.x - curr.rect.x;
            const deltaWidth = prev.rect.width - curr.rect.width;
            const widthAdjustment = Math.abs(deltaWidth / 2);

            const x = deltaX + (deltaWidth < 0 ? -widthAdjustment : widthAdjustment);
            const scaleX = prev.rect.width / curr.rect.width;

            const enter = {
                x: [x, 0], // from a translated pos back to the original (0)
                scaleX: [scaleX, 1] // from a scaled size back to the original (1)
            };

            const transition = {
                onUpdate: latest => {
                    curr.rect = ref.getBoundingClientRect();
                }
            };

            props = mergeDeep(props || {}, { enter, transition });

            // Animate from prev -> curr
            await animateEnter(ref, props);
        }
    } catch (error) {
        console.error("animateLayoutId:", error);
    }
}

async function animateCore(ref, props, key) {
    if (!(ref instanceof HTMLElement)) {
        throw new Error("Invalid element provided");
    }

    const { transition } = props || {};
    const animationProps = props?.[key];

    await motionAnimate(ref, animationProps, transition);
}

export const motion = {
    animateEnter,
    animateExit,
    animateLayoutId
}