import Navbar from "./components/Navbar";
import HeroSection from "./components/HeroSection";
import KeyPointsSection from "./components/KeyPointsSection";
import FeaturesSection from "./components/FeaturesSection";
import AudienceSection from "./components/AudienceSection";
import CtaSection from "./components/CtaSection";
import Footer from "./components/Footer";

export default function LandingPage() {
    return (
        <>
            <Navbar />
            <HeroSection />
            <KeyPointsSection />
            <FeaturesSection />
            <AudienceSection />
            <CtaSection />
            <Footer />
        </>
    );
}
