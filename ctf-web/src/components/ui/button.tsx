import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

/**
 * Button — composant de démonstration shadcn/ui configuré à la charte Sentys (teal).
 * Pattern shadcn : cva (variantes) + cn (fusion de classes) + Radix Slot (asChild).
 * Charte : primary = var(--accent) (bg-sentys), hover = var(--accent-hover) (bg-sentys-dark),
 * accent = var(--accent) (bg-sentys-accent). Cibles tactiles ≥ 44px (mobile-first).
 * Contraste WCAG AA : texte blanc sur teal (var(--accent)) ≈ 3.0:1 pour un gros bouton ;
 * pour du petit texte préférer bg-sentys-dark (var(--accent-hover)) ≈ 4.6:1.
 */
const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg text-sm font-medium transition-colors duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sentys/50 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default: "bg-sentys text-white hover:bg-sentys-dark",
        accent: "bg-sentys-accent text-[var(--surface-2)] hover:bg-sentys",
        outline: "border border-sentys text-sentys bg-transparent hover:bg-sentys/10",
        ghost: "text-sentys hover:bg-sentys/10",
        secondary: "bg-[#F1F5F9] text-fg-body hover:bg-[#E2E8F0]",
        destructive: "bg-danger text-white hover:bg-[#DC2626]",
        link: "text-sentys underline-offset-4 hover:underline",
      },
      size: {
        default: "h-11 min-h-[44px] px-4 py-2",
        sm: "h-11 min-h-[44px] px-3",
        lg: "h-12 min-h-[48px] px-6 text-base",
        icon: "h-11 w-11 min-h-[44px] min-w-[44px]",
      },
    },
    defaultVariants: { variant: "default", size: "default" },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button";
    return (
      <Comp className={cn(buttonVariants({ variant, size, className }))} ref={ref} {...props} />
    );
  }
);
Button.displayName = "Button";

export { Button, buttonVariants };
