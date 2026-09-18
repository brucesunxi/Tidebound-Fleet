//
//  SwAppStoreConnector.m
//  SupersonicWisdom
//
//  Created by Igor Kheisson on 11/04/2024.
//

#import "SwAppStoreConnector.h"

@implementation SwAppStoreConnector

+ (void)openAppStorePageWithAppId:(NSString *)appId {
    SKStoreProductViewController *storeViewController = [[SKStoreProductViewController alloc] init];
    SwAppStoreConnector *connector = [[SwAppStoreConnector alloc] init];
    storeViewController.delegate = connector;
    
    NSDictionary *parameters = @{ SKStoreProductParameterITunesItemIdentifier: appId };
    UIViewController *presentingViewController = [UIApplication sharedApplication].keyWindow.rootViewController;
    while (presentingViewController.presentedViewController) {
        presentingViewController = presentingViewController.presentedViewController;
    }

    [storeViewController loadProductWithParameters:parameters completionBlock:^(BOOL result, NSError * _Nullable error) {
        if (error) {
            NSLog(@"Error %@ with User Info %@.", error, [error userInfo]);
        } else {
            // Present the store product view controller
            dispatch_async(dispatch_get_main_queue(), ^{
                [presentingViewController presentViewController:storeViewController animated:YES completion:nil];
            });
        }
    }];
}

- (void)productViewControllerDidFinish:(SKStoreProductViewController *)viewController {
    [viewController dismissViewControllerAnimated:YES completion:nil];
}

@end

