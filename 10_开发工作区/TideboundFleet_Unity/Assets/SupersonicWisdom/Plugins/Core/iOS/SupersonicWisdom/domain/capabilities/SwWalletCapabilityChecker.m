//
//  WalletCapabilityChecker.m
//  SupersonicWisdom
//
//  Created by Yuval Ozeri on 16/05/2024.
//

#import <Foundation/Foundation.h>
#import <PassKit/PassKit.h>

#import "SwWalletCapabilityChecker.h"

@implementation SwWalletCapabilityChecker

+ (BOOL)isCardInWallet {
    
    if ([PKPaymentAuthorizationViewController canMakePayments]) {
        
        if (@available(iOS 10.0, *)) {
            NSArray *availableNetworks = [PKPaymentRequest availableNetworks];
            return [PKPaymentAuthorizationViewController canMakePaymentsUsingNetworks:availableNetworks];
        } else {
            NSMutableArray *supportedNetworks = [NSMutableArray arrayWithArray:@[
                PKPaymentNetworkVisa,
                PKPaymentNetworkMasterCard,
                PKPaymentNetworkAmex,
            ]];
            
            if (@available(iOS 9.0, *)) {
                [supportedNetworks addObject:PKPaymentNetworkDiscover];
                [supportedNetworks addObject:PKPaymentNetworkPrivateLabel];
            }

            if (@available(iOS 9.2, *)) {
                [supportedNetworks addObject:PKPaymentNetworkChinaUnionPay];
                [supportedNetworks addObject:PKPaymentNetworkInterac];
            }

            return [PKPaymentAuthorizationViewController canMakePaymentsUsingNetworks:supportedNetworks];
        }
    }
    
    return NO;
}

@end
