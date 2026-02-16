import { defineConfig } from 'vitepress'

export default defineConfig({
  title: "lip",
  description: "A general package installer",
  themeConfig: {
    nav: [
      { text: 'Intro', link: '/intro/quick_start' },
      { text: 'Concepts', link: '/concepts/architecture' },
      { text: 'Guides', link: '/guides/creating_packages' },
      { text: 'CLI', link: '/cli/overview' },
      { text: 'Daemon', link: '/daemon/overview' }
    ],

    sidebar: {
      '/intro/': [
        {
          text: 'Introduction',
          items: [
            { text: 'Installation', link: '/intro/installation' },
            { text: 'Quick Start', link: '/intro/quick_start' }
          ]
        }
      ],
      '/concepts/': [
        {
          text: 'Concepts',
          items: [
            { text: 'Architecture', link: '/concepts/architecture' },
            { text: 'Package Manifest', link: '/concepts/package_manifest' },
            { text: 'Lockfiles', link: '/concepts/lockfiles' }
          ]
        }
      ],
      '/cli/': [
        {
          text: 'CLI Reference',
          items: [
            { text: 'Overview', link: '/cli/overview' },
            { text: 'Configuration', link: '/cli/configuration' },
            {
              text: 'Commands',
              items: [
                { text: 'cache clean', link: '/cli/commands/cache_clean' },
                { text: 'config', link: '/cli/commands/config' },
                { text: 'init', link: '/cli/commands/init' },
                { text: 'install', link: '/cli/commands/install' },
                { text: 'list', link: '/cli/commands/list' },
                { text: 'migrate', link: '/cli/commands/migrate' },
                { text: 'uninstall', link: '/cli/commands/uninstall' },
                { text: 'update', link: '/cli/commands/update' },
                { text: 'version', link: '/cli/commands/version' },
                { text: 'view', link: '/cli/commands/view' }
              ]
            }
          ]
        }
      ],
      '/daemon/': [
        {
          text: 'Daemon (lipd)',
          items: [
            { text: 'Overview', link: '/daemon/overview' },
            { text: 'JSON-RPC Spec', link: '/daemon/json_rpc_spec' },
            {
              text: 'Commands',
              items: [
                { text: 'run', link: '/daemon/commands/run' }
              ]
            }
          ]
        }
      ],
      '/guides/': [
        {
          text: 'How-to Guides',
          items: [
            { text: 'Creating Packages', link: '/guides/creating_packages' },
            { text: 'Migrating Packages', link: '/guides/migrating_packages' },
            { text: 'Installing Dependencies', link: '/guides/installing_dependencies' },
            { text: 'Managing Configuration', link: '/guides/managing_configuration' },
            { text: 'Internal Mechanism', link: '/guides/internal_mechanism' }
          ]
        }
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/futrime/lip' }
    ]
  }
})
