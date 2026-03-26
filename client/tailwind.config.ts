import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Segoe UI Variable"', '"Trebuchet MS"', "sans-serif"],
        display: ['"Bahnschrift"', '"Segoe UI Variable"', "sans-serif"],
      },
      colors: {
        ink: "#13262f",
        mist: "#f3f0ea",
        ember: "#c96b32",
        lagoon: "#0f766e",
        sand: "#efe6d8",
      },
      boxShadow: {
        panel: "0 18px 50px -24px rgba(19, 38, 47, 0.25)",
      },
      backgroundImage: {
        "mesh-warm":
          "radial-gradient(circle at top left, rgba(201,107,50,0.22), transparent 28%), radial-gradient(circle at top right, rgba(15,118,110,0.18), transparent 26%), linear-gradient(135deg, #f6f0e7 0%, #ffffff 42%, #eef5f3 100%)",
      },
    },
  },
  plugins: [],
} satisfies Config;
