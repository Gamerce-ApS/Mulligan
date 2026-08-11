#import <UIKit/UIKit.h>

extern "C" {
	bool isHapticEngineSupported() {
        return [[[UIDevice currentDevice] valueForKey:@"_feedbackSupportLevel"] isEqualToNumber:@2];
    }

	void SelectionFeedbackGenerator() {
        UISelectionFeedbackGenerator *generator = [UISelectionFeedbackGenerator new];
        [generator prepare];
        [generator selectionChanged];
    }

    void ImpactFeedbackGenerator(int style, float intensity) {
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyle) style];
        [generator prepare];
        
        if (intensity >= 0) { // Has Intensity
            if (@available(iOS 13.0, *)) {
                [generator impactOccurredWithIntensity:CGFloat(intensity)];
            } else {
                [generator impactOccurred];
            }
        } else {
            [generator impactOccurred];
        }
    }

    void NotificationFeedbackGenerator(int style) {
        UINotificationFeedbackGenerator *generator = [UINotificationFeedbackGenerator new];
        [generator prepare];
        [generator notificationOccurred:(UINotificationFeedbackType) style];
    }
}
