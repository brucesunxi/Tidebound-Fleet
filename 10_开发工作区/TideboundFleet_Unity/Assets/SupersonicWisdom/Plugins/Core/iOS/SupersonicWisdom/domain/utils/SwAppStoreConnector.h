//
//  SwAppStoreConnector.h
//  SupersonicWisdom
//
//  Created by Igor Kheisson on 11/04/2024.
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>

@interface SwAppStoreConnector : NSObject <SKStoreProductViewControllerDelegate>

+ (void)openAppStorePageWithAppId:(NSString *)appId;

@end
